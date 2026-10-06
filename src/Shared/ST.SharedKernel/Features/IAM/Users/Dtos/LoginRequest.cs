namespace ST.SharedKernel.Features.IAM.Users.Dtos;
public abstract record LoginRequest(
    string UserName,
    string Password,
    bool RememberMe,
    string? ReturnUrl,
    string DeviceFingerprint
);
