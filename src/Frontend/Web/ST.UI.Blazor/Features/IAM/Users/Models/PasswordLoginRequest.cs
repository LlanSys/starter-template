using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.UI.Blazor.Features.IAM.Users.Models;

internal sealed record PasswordLoginRequest(
    string UserName,
    string Password,
    bool RememberMe,
    string? ReturnUrl,
    string DeviceFingerprint)
    : LoginRequest(UserName, Password, RememberMe, ReturnUrl, DeviceFingerprint);
