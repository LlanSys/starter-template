using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.Banking.Customers.Mappings;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Application.Features.Shared.EmailTemplates.Mappings;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.HR.Employees.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.HR.Employees.Enums;
using ST.Domain.Features.IAM.Users.Enums;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class CreateAppUser(
    UserManager<AppUser> userManager,
    IBankingUnitOfWork bankingUnitOfWork,
    IHrUnitOfWork hrUnitOfWork,
    IIamUnitOfWork iamUnitOfWork,
    ILogger<CreateAppUser> logger) : IRequestHandler<CreateAppUserCommand, AppResponse<AppUserResponse>>
{
    public async Task<AppResponse<AppUserResponse>> Handle(CreateAppUserCommand command, CancellationToken ct)
    {
        var req = command.Request;
        AppUser? createdUser = null;

        try
        {
            if (req.EmployeeId.HasValue && req.CustomerId.HasValue)
                return AppResponses.Failure<AppUserResponse>("A user account can be linked to either an employee or a customer, not both.");

            Employee? employee = null;

            if (req.EmployeeId.HasValue)
            {
                employee = await hrUnitOfWork.EmployeeRepository
                    .FindByCondition(e => e.Id == req.EmployeeId.Value)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (employee is null)
                    return AppResponses.Failure<AppUserResponse>("The specified employee does not exist or has been deactivated.");

                var alreadyLinked = await userManager.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.EmployeeId == req.EmployeeId.Value, ct)
                    .ConfigureAwait(false);

                if (alreadyLinked)
                    return AppResponses.Failure<AppUserResponse>("A user account already exists for this employee.");
            }

            Customer? customer = null;
            if (req.CustomerId.HasValue)
            {
                customer = await bankingUnitOfWork.CustomerRepository
                    .FindByCondition(c => c.Id == req.CustomerId.Value)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (customer is null)
                    return AppResponses.Failure<AppUserResponse>("The specified customer does not exist or has been deactivated.");

                var alreadyLinked = await userManager.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.CustomerId == req.CustomerId.Value, ct)
                    .ConfigureAwait(false);

                if (alreadyLinked)
                    return AppResponses.Failure<AppUserResponse>("A user account already exists for this customer.");
            }

            var emailOrUsernameExists = await userManager.Users
                .AsNoTracking()
                .AnyAsync(u => u.UserName == req.Username || u.Email == req.Email, ct)
                .ConfigureAwait(false);

            if (emailOrUsernameExists)
                return AppResponses.Failure<AppUserResponse>("An account with this username or email already exists.");

            var appUser = employee is not null
                ? AppUser.CreateForEmployee(
                    Guid.Empty,
                    employee.Id,
                    req.Username,
                    employee.FirstName,
                    employee.LastName,
                    employee.Email,
                    employee.PhoneNumber,
                    req.IdNumber ?? employee.IdNumber,
                    createdBy: "System")
                : customer is not null
                    ? AppUser.CreateForCustomer(
                        Guid.Empty,
                        customer.Id,
                        req.Username,
                        req.FirstName,
                        req.LastName,
                        req.Email,
                        req.PhoneNumber ?? string.Empty,
                        req.IdNumber ?? string.Empty,
                        createdBy: "System")
                    : AppUser.Create(
                    Guid.Empty,
                    employeeId: null,
                    req.Username,
                    req.FirstName,
                    req.LastName,
                    req.Email,
                    req.PhoneNumber ?? string.Empty,
                    createdBy: "System");

            appUser.SetIdentityProfile(
                req.IdNumber ?? appUser.NationalId,
                Enum.TryParse<Gender>(req.Gender, true, out var g) ? g : Gender.Other);

            var identityResult = await userManager
                .CreateAsync(appUser, req.Password)
                .ConfigureAwait(false);

            if (!identityResult.Succeeded)
            {
                var error = identityResult.Errors.First().Description;
                ServiceLogDefinitions.LogAppUserCreationWarning(logger, req.Email, error);
                return AppResponses.Failure<AppUserResponse>(error);
            }

            createdUser = appUser;

            await iamUnitOfWork.ExecuteInTransactionWithRetryAsync(async () =>
            {
                if (req.Roles?.Any() == true)
                {
                    var roleResult = await userManager
                        .AddToRolesAsync(appUser, req.Roles)
                        .ConfigureAwait(false);

                    if (!roleResult.Succeeded)
                        throw new InvalidOperationException(
                            string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }

                var profile = AppUserProfile.Create(
                    appUser.Id,
                    appUser.PhoneNumber,
                    appUser.PhoneNumber,
                    appUser.Email,
                    "System");

                await iamUnitOfWork.AppUserProfileRepository
                    .CreateOrUpdateAsync(appUser.Id, profile, ct)
                    .ConfigureAwait(false);

                return true;
            }).ConfigureAwait(false);

            appUser.RaiseAppUserCreatedEvent();

            _ = await userManager.GenerateEmailConfirmationTokenAsync(appUser).ConfigureAwait(false);

            ServiceLogDefinitions.LogAppUserCreated(logger, appUser.Id, appUser.Email ?? string.Empty);

            return AppResponses.Success("User created successfully.", appUser.ToAppUserResponse());
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogAppUserCreationFailed(logger, req.Email, ex);

            if (createdUser is not null)
            {
                try
                {
                    await userManager.DeleteAsync(createdUser).ConfigureAwait(false);
                    ServiceLogDefinitions.LogIdentityRollbackSucceeded(logger, req.Email);
                }
                catch (Exception)
                {
                    ServiceLogDefinitions.LogIdentityRollbackCritical(logger, createdUser.Id, req.Email);
                }
            }

            throw;
        }
    }
}
