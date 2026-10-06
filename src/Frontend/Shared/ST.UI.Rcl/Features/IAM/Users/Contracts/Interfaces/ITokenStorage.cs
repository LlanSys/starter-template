using System;
using System.Collections.Generic;
using System.Text;

namespace ST.UI.Rcl.Features.IAM.Users.Contracts.Interfaces;

public interface ITokenStorage
{
    Task<bool> ClearAsync();
    Task<(string? AccessToken, string? RefreshToken, string? SessionId)> GetAsync();
    Task<bool> SaveAsync(string? accessToken, string? refreshToken, string? sessionId);
}
