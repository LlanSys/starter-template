using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Dtos.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.IAM.Users.Contracts.Interfaces;

public interface IAppUserService
{
    Task<bool> IsInRoleAsync(AppUser appUser, string roleName);
    Task<bool> DeleteAsync(AppUser appUser);
    Task<bool> UpdateAsync(AppUser user);
    Task<AppUser?> FindByIdAsync(string appUserId);
    Task<AppUser?> FindByEmailAsync(string email);
    Task<(bool Success, string Message)> CreateAsync(AppUser user, string password);
}
