using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.Api.Features.IAM.Users.Controllers;

public sealed record LoginApiRequest(
    string UserName,
    string Password,
    bool RememberMe,
    string? ReturnUrl,
    string DeviceFingerprint)
    : LoginRequest(UserName, Password, RememberMe, ReturnUrl, DeviceFingerprint);
