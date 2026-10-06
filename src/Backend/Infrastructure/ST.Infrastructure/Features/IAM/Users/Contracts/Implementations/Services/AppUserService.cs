using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Dtos.Common;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;

internal sealed class AppUserService(UserManager<AppUser> userManager) : IAppUserService
{
    private readonly UserManager<AppUser> _userManager = userManager;

    public async Task<(bool Success, string Message)> CreateAsync(AppUser user, string password)
    {
        var result = await _userManager.CreateAsync(user, password).ConfigureAwait(false);
        if (result.Succeeded)
        {
            return (true, string.Empty);
        }

        return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<bool> DeleteAsync(AppUser appUser)
    {
        appUser.MarkAsDeleted("System");
        var result = await _userManager.UpdateAsync(appUser).ConfigureAwait(false);
        return result.Succeeded;
    }

    public async Task<AppUser?> FindByEmailAsync(string email)
        => await _userManager.FindByEmailAsync(email).ConfigureAwait(false);

    public async Task<AppUser?> FindByIdAsync(string appUserId)
        => await _userManager.FindByIdAsync(appUserId).ConfigureAwait(false);

    public async Task<bool> IsInRoleAsync(AppUser appUser, string roleName)
        => await _userManager.IsInRoleAsync(appUser, roleName).ConfigureAwait(false);

    public async Task<bool> UpdateAsync(AppUser user)
    {
        var result = await _userManager.UpdateAsync(user).ConfigureAwait(false);
        return result.Succeeded;
    }
}
