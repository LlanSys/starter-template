using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Notifications.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class GrantEmployeeSystemAccess(
    UserManager<AppUser> userManager,
    IHrUnitOfWork hrUnitOfWork,
    IOptions<IamProvisioningSettings> provisioningOptions,
    IEmailService emailService,
    ILogger<GrantEmployeeSystemAccess> logger)
    : IRequestHandler<GrantEmployeeSystemAccessCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(GrantEmployeeSystemAccessCommand command, CancellationToken ct)
    {
        var employeeId = command.EmployeeId.ToString();

        try
        {
            var temporaryPassword = provisioningOptions.Value.TemporaryPassword;
            if (string.IsNullOrWhiteSpace(temporaryPassword))
            {
                return AppResponses.Failure<bool>(
                    "IAM provisioning is not configured. Set IamProvisioning:TemporaryPassword via user secrets, environment variables, or Key Vault.");
            }

            var user = await userManager.Users
                .SingleOrDefaultAsync(u => u.EmployeeId == command.EmployeeId, ct)
                .ConfigureAwait(false);

            var createdNewUser = false;
            if (user is null)
            {
                var employee = await hrUnitOfWork.EmployeeRepository
                    .FindByCondition(existing => existing.Id == command.EmployeeId)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (employee is null)
                {
                    return AppResponses.Failure<bool>("Employee record was not found.");
                }

                user = AppUser.CreateForEmployee(
                    Guid.Empty,
                    employee.Id,
                    employee.Email,
                    employee.FirstName,
                    employee.LastName,
                    employee.Email,
                    employee.PhoneNumber,
                    employee.IdNumber,
                    command.GrantedBy);

                user.EmailConfirmed = true;

                var createResult = await userManager.CreateAsync(user, temporaryPassword).ConfigureAwait(false);
                if (!createResult.Succeeded)
                {
                    return AppResponses.Failure<bool>(createResult.Errors.First().Description);
                }

                createdNewUser = true;
            }

            if (user.IsActive)
            {
                return AppResponses.Failure<bool>(
                    "This employee is already linked to an active IAM account. Use Manage Roles or Revoke Access instead.");
            }

            if (!createdNewUser)
            {
                var passwordResetResult = await ResetTemporaryPasswordForReactivationAsync(user, temporaryPassword)
                    .ConfigureAwait(false);
                if (!passwordResetResult.Succeeded)
                {
                    return AppResponses.Failure<bool>(passwordResetResult.Errors.First().Description);
                }
            }

            user.GrantAccess(command.GrantedBy, command.Roles);
            user.EmailConfirmed = true;

            var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
            if (!updateResult.Succeeded)
                return AppResponses.Failure<bool>(updateResult.Errors.First().Description);

            var rolesAdded = Array.Empty<string>();
            if (command.Roles.Any())
            {
                var existingRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
                var rolesToAdd = command.Roles
                    .Except(existingRoles, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (rolesToAdd.Length > 0)
                {
                    var roleResult = await userManager.AddToRolesAsync(user, rolesToAdd).ConfigureAwait(false);
                    if (!roleResult.Succeeded)
                        return AppResponses.Failure<bool>(roleResult.Errors.First().Description);

                    rolesAdded = rolesToAdd;
                }
            }

            var emailResponse = await SendActivationEmailAsync(user, provisioningOptions.Value, createdNewUser, ct)
                .ConfigureAwait(false);
            if (!emailResponse.IsSuccess)
            {
                await RollBackActivationAsync(user, rolesAdded, command.GrantedBy).ConfigureAwait(false);
                return AppResponses.Failure<bool>("System access was not granted because the activation email could not be sent. Please verify email settings and try again.");
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                ServiceLogDefinitions.LogEmployeeSystemAccessGranted(logger, employeeId, command.GrantedBy);
            }

            var message = createdNewUser
                ? "IAM account created, linked to the employee, activated, roles assigned, and activation email sent."
                : "Existing IAM account reactivated, roles assigned, and activation email sent.";

            return AppResponses.Success(message, true);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogGrantEmployeeSystemAccessError(logger, employeeId, ex);
            throw;
        }
    }

    private async Task<IdentityResult> ResetTemporaryPasswordForReactivationAsync(
        AppUser user,
        string temporaryPassword)
    {
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, temporaryPassword).ConfigureAwait(false);
        if (!resetResult.Succeeded)
        {
            return resetResult;
        }

        user.RequirePasswordChange = true;
        user.PasswordLastChanged = null;
        user.ResetFailedLoginAttempts();

        var failedCountResult = await userManager.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
        if (!failedCountResult.Succeeded)
        {
            return failedCountResult;
        }

        return await userManager.SetLockoutEndDateAsync(user, null).ConfigureAwait(false);
    }

    private Task<AppResponse<SendEmailResponse>> SendActivationEmailAsync(
        AppUser user,
        IamProvisioningSettings settings,
        bool createdNewUser,
        CancellationToken ct)
    {
        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = user.UserName ?? user.Email ?? "there";
        }

        var subject = createdNewUser
            ? "Your StarterTemplate account is ready"
            : "Your StarterTemplate access has been reactivated";

        var body = $"""
            <p>Hello {System.Net.WebUtility.HtmlEncode(displayName)},</p>
            <p>Your system access has been enabled.</p>
            <p><strong>Username:</strong> {System.Net.WebUtility.HtmlEncode(user.UserName ?? user.Email ?? string.Empty)}</p>
            <p><strong>Temporary password:</strong> {System.Net.WebUtility.HtmlEncode(settings.TemporaryPassword)}</p>
            <p>Please sign in at <a href="{System.Net.WebUtility.HtmlEncode(settings.SignInUrl)}">{System.Net.WebUtility.HtmlEncode(settings.SignInUrl)}</a> and change your password immediately.</p>
            <p>If you did not expect this access, contact your system administrator.</p>
            """;

        return emailService.SendEmailAsync(new SendEmailRequest
        {
            To = user.Email,
            Subject = subject,
            Body = body
        }, ct);
    }

    private async Task RollBackActivationAsync(AppUser user, string[] rolesAdded, string changedBy)
    {
        if (rolesAdded.Length > 0)
        {
            var roleRemovalResult = await userManager.RemoveFromRolesAsync(user, rolesAdded).ConfigureAwait(false);
            if (!roleRemovalResult.Succeeded)
            {
                ServiceLogDefinitions.LogGrantEmployeeSystemAccessError(
                    logger,
                    user.EmployeeId?.ToString() ?? user.Id,
                    new InvalidOperationException(string.Join("; ", roleRemovalResult.Errors.Select(error => error.Description))));
            }
        }

        if (user.IsActive)
        {
            user.RevokeAccess(changedBy, "Activation email could not be sent.");
            var revokeResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
            if (!revokeResult.Succeeded)
            {
                ServiceLogDefinitions.LogGrantEmployeeSystemAccessError(
                    logger,
                    user.EmployeeId?.ToString() ?? user.Id,
                    new InvalidOperationException(string.Join("; ", revokeResult.Errors.Select(error => error.Description))));
            }
        }
    }
}
