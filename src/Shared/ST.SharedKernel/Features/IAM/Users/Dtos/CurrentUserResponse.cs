namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record CurrentUserResponse(
    string AppUserId,
    Guid TenantId,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool? IsAuthenticated,
    DateTimeOffset? LastLoginAt,
    List<string> Roles,
    List<string>? Permissions = null,
    string? SessionId = null,
    bool MfaEnrollmentRequired = false,
    Uri? ProfilePictureUrl = null)
{
    public bool AdministratorSetupRequired { get; init; }
    public Guid? SupportGrantId { get; init; }
    public bool IsReadOnlySupport { get; init; }
    public DateTimeOffset? SupportExpiresAt { get; init; }
    public string? WorkspaceName { get; init; }

    public IReadOnlyList<string> UnavailablePermissions { get; init; } = [];
    public IReadOnlyList<string> AvailablePages { get; init; } = [];
    public IReadOnlyList<string> EnabledModules { get; init; } = [];
    public bool IsPlatformStaff { get; init; }
    public bool IsReadOnlyDemo { get; init; }
    public bool IsDemoWorkspace { get; init; }
}
