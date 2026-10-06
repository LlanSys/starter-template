using ST.Domain.Features.IAM.Users.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ST.Application.Features.IAM.Users.Contracts.Interfaces;

public interface IClaimsService
{
    Task<List<Claim>> GetUserClaimsAsync(AppUser appUser, Guid? sessionId = null);
    Task<List<Claim>> GetUserClaimsCombinedAsync(AppUser user);
    Task<bool> AddUserClaimAsync(AppUser user, Claim claim);
    Task<bool> RemoveUserClaimAsync(AppUser user, Claim claim);
    Task<bool> UpdateUserClaimAsync(AppUser user, Claim existingClaim, Claim newClaim);
}
