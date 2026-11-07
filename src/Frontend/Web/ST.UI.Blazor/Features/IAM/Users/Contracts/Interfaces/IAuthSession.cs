using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;

public interface IAuthSession
{
    event Action? AuthenticationChanged;
    bool IsInitialized { get; }
    bool IsAuthenticated { get; }
    CurrentUserResponse? CurrentUser { get; }
    string? ProfilePictureDataUri { get; }
    string? PendingTwoFactorUserId { get; }
    string? LastError { get; }
    bool HasFullAccess { get; }
    bool MfaEnrollmentRequired { get; }

    Task InitializeAsync();
    Task RefreshAsync();
    Task<AppResponse<LoginResponse>> SignInAsync(LoginRequest request);
    Task<AppResponse<LoginResponse>> SignInWithPasskeyAsync(LoginWithPasskeyRequest request);
    Task<AppResponse<LoginResponse>> ExchangeSsoCodeAsync(string code);
    Task<AppResponse<VerifyOtpResponse>> CompleteTwoFactorAsync(VerifyOtpRequest request);
    Task<AppResponse<bool>> SignOutAsync();
    bool HasPermission(string permission);
    bool HasAnyPermission(params string[] permissions);
    bool IsInRole(string role);
}
