using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Users.Enums;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace ST.Infrastructure.Features.IAM.Users.Seeding;

internal sealed class DevelopmentIdentitySeeder(
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    IOptions<DevelopmentSeedSettings> options,
    ILogger<DevelopmentIdentitySeeder> logger)
{
    private const string BootstrapSeedActorId = "00000000-0000-0000-0000-000000000001";
    private readonly DevelopmentSeedSettings _settings = options.Value;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Validator.ValidateObject(_settings, new ValidationContext(_settings), validateAllProperties: true);
            if (_settings.TenantId == Guid.Empty)
            {
                throw new InvalidOperationException("DevelopmentSeed:TenantId must identify the bootstrap tenant.");
            }

            var userName = _settings.AdminUserName.Trim();
            var email = _settings.AdminEmail.Trim();
            var normalizedName = userManager.NormalizeName(userName);
            var normalizedEmail = userManager.NormalizeEmail(email);
            var user = await userManager.Users
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    existing => existing.NormalizedUserName == normalizedName || existing.NormalizedEmail == normalizedEmail,
                    cancellationToken)
                .ConfigureAwait(false);

            if (user is not null &&
                (user.TenantId != _settings.TenantId ||
                 !string.Equals(user.UserName, userName, StringComparison.OrdinalIgnoreCase) ||
                 !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase) ||
                 user.IsDeleted || !user.IsActive))
            {
                throw new InvalidOperationException("The configured bootstrap identity conflicts with an existing account. Resolve it through account administration.");
            }

            var adminRole = Roles.SysAdmin.ToDisplayString();
            if (!await roleManager.RoleExistsAsync(adminRole).ConfigureAwait(false))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new AppRole { Name = adminRole }).ConfigureAwait(false), "create the bootstrap administrator role");
            }

            if (user is null)
            {
                user = AppUser.CreateSystemUser(_settings.TenantId, userName, email, BootstrapSeedActorId);
                user.FirstName = "Administrator";
                user.IsActive = true;
                user.EmailConfirmed = true;
                user.ActivatedAt = DateTimeOffset.UtcNow;
                user.ActivatedBy = BootstrapSeedActorId;
                EnsureSucceeded(await userManager.CreateAsync(user, _settings.AdminPassword).ConfigureAwait(false), "create the bootstrap administrator");
            }
            else if (_settings.ResetExistingAdminPassword)
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
                EnsureSucceeded(await userManager.ResetPasswordAsync(user, resetToken, _settings.AdminPassword).ConfigureAwait(false), "reset the bootstrap administrator password");
            }

            if (!await userManager.IsInRoleAsync(user, adminRole).ConfigureAwait(false))
            {
                EnsureSucceeded(await userManager.AddToRoleAsync(user, adminRole).ConfigureAwait(false), "assign the bootstrap administrator role");
            }

            ServiceLogDefinitions.LogDevelopmentAdminSeeded(logger, email);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogDevelopmentIdentitySeedError(logger, ex);
            throw;
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            // Provider details remain in structured internal diagnostics, never API responses.
            throw new InvalidOperationException($"Failed to {operation}: {string.Join("; ", result.Errors.Select(error => error.Code))}");
        }
    }
}
