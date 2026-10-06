using Microsoft.Extensions.Logging;
using System;

namespace ST.Infrastructure.Logging;


internal static partial class ServiceLogDefinitions
{
    [LoggerMessage(EventId = 2501, Level = LogLevel.Error, Message = "Failed to link external login for user {Email}")]
    public static partial void LogSsoExternalLoginLinkFailed(ILogger logger, string email);

    [LoggerMessage(EventId = 2502, Level = LogLevel.Error, Message = "Failed to create SSO user for {Email}: {Errors}")]
    public static partial void LogSsoUserCreationError(ILogger logger, string email, string errors);

    [LoggerMessage(EventId = 2506, Level = LogLevel.Error, Message = "Passkey assertion threw an exception for user {Username}")]
    public static partial void LogPasskeyAssertionException(ILogger logger, string username, Exception ex);

    [LoggerMessage(EventId = 2503, Level = LogLevel.Warning, Message = "SendGrid rejected the email payload. Status: {StatusCode}. Response: {Response}")]
    public static partial void LogSendGridRejection(ILogger logger, System.Net.HttpStatusCode statusCode, string response);

    [LoggerMessage(EventId = 2504, Level = LogLevel.Warning, Message = "ACS rejected the email payload. Status: {Status}. Error: {Message}")]
    public static partial void LogAcsRejection(ILogger logger, int status, string message);

    [LoggerMessage(EventId = 2505, Level = LogLevel.Warning, Message = "Resend rejected the email payload. Status: {StatusCode}. Response: {Response}")]
    public static partial void LogResendRejection(ILogger logger, System.Net.HttpStatusCode statusCode, string response);
    [LoggerMessage(EventId = 3400, Level = LogLevel.Error, Message = "Error composing email for template {EmailTemplate}")]
    public static partial void LogEmailComposeError(ILogger logger, string emailTemplate, Exception ex);

    [LoggerMessage(EventId = 3401, Level = LogLevel.Error, Message = "Error checking concurrent sessions for user: {UserId}")]
    public static partial void LogSessionConcurrentCheckError(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3402, Level = LogLevel.Error, Message = "Failed to end old sessions for user {UserId}: {Message}")]
    public static partial void LogFailedToEndOldSessions(ILogger logger, string userId, string message);

    [LoggerMessage(EventId = 3405, Level = LogLevel.Error, Message = "Error creating session for user: {UserId}")]
    public static partial void LogSessionCreateError(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3410, Level = LogLevel.Error, Message = "Error ending session: {SessionId}")]
    public static partial void LogSessionEndError(ILogger logger, string sessionId, Exception ex);

    [LoggerMessage(EventId = 3411, Level = LogLevel.Error, Message = "Error ending all sessions for user: {UserId}")]
    public static partial void LogEndAllSessionsError(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3412, Level = LogLevel.Error, Message = "Error getting active sessions for user: {UserId}")]
    public static partial void LogGetActiveSessionsError(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3414, Level = LogLevel.Error, Message = "Error during session cleanup")]
    public static partial void LogSessionCleanupError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3415, Level = LogLevel.Error, Message = "Error validating session: {SessionId}")]
    public static partial void LogSessionValidationError(ILogger logger, string sessionId, Exception ex);

    [LoggerMessage(EventId = 3416, Level = LogLevel.Error, Message = "Error initiating TOTP setup for user {UserId}")]
    public static partial void LogTotpSetupInitiationError(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3417, Level = LogLevel.Error, Message = "Error finalizing TOTP setup for user {UserId}")]
    public static partial void LogTotpSetupFinalizationError(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3418, Level = LogLevel.Error, Message = "Error verifying TOTP code for user {UserId}")]
    public static partial void LogTotpVerificationError(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3419, Level = LogLevel.Error, Message = "Error verifying TOTP code")]
    public static partial void LogTotpCodeVerificationError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3420, Level = LogLevel.Error, Message = "Error verifying TOTP code with plain text secret")]
    public static partial void LogTotpPlainTextCodeVerificationError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3422, Level = LogLevel.Error, Message = "TOTP secret decryption failed for user {UserId}; verification was denied without changing IAM state.")]
    public static partial void LogTotpSecretDecryptionFailure(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3421, Level = LogLevel.Information, Message = "Added claim - {ClaimType}:{ClaimValue} to user {UserId}")]
    public static partial void LogClaimAdded(ILogger logger, string claimType, string claimValue, string userId);

    [LoggerMessage(EventId = 3485, Level = LogLevel.Warning, Message = "Failed to add claim {ClaimType}:{ClaimValue} to user {UserId}: {Errors}")]
    public static partial void LogFailedToAddClaim(ILogger logger, string claimType, string claimValue, string userId, string errors);

    [LoggerMessage(EventId = 3423, Level = LogLevel.Error, Message = "Error adding claim to user {UserId}")]
    public static partial void LogErrorAddingClaim(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3424, Level = LogLevel.Information, Message = "Removed claim {ClaimType}:{ClaimValue} from user {UserId}")]
    public static partial void LogClaimRemoved(ILogger logger, string claimType, string claimValue, string userId);

    [LoggerMessage(EventId = 3425, Level = LogLevel.Warning, Message = "Failed to remove claim {ClaimType}:{ClaimValue} from user {UserId}: {Errors}")]
    public static partial void LogFailedToRemoveClaim(ILogger logger, string claimType, string claimValue, string userId, string errors);

    [LoggerMessage(EventId = 3426, Level = LogLevel.Error, Message = "Error removing claim from user {UserId}")]
    public static partial void LogErrorRemovingClaim(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3427, Level = LogLevel.Warning, Message = "Failed to remove existing claim for user {UserId}")]
    public static partial void LogFailedToRemoveExistingClaim(ILogger logger, string userId);

    [LoggerMessage(EventId = 3428, Level = LogLevel.Warning, Message = "Failed to add new claim for user {UserId}, rolled back")]
    public static partial void LogFailedToAddNewClaimRolledBack(ILogger logger, string userId);

    [LoggerMessage(EventId = 3429, Level = LogLevel.Information, Message = "Updated claim for user {UserId}: {OldClaim} -> {NewClaim}")]
    public static partial void LogUpdatedClaim(ILogger logger, string userId, string oldClaim, string newClaim);

    [LoggerMessage(EventId = 3430, Level = LogLevel.Error, Message = "Error updating claim for user {UserId}")]
    public static partial void LogErrorUpdatingClaim(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3431, Level = LogLevel.Information, Message = "Email OTP sent to user {UserId} for {Purpose}")]
    public static partial void LogEmailOtpSent(ILogger logger, string userId, string purpose);

    [LoggerMessage(EventId = 3432, Level = LogLevel.Error, Message = "Error generating refresh token for user {UserId}")]
    public static partial void LogFailedToGenerateRefreshToken(ILogger logger, string userId);

    [LoggerMessage(EventId = 3433, Level = LogLevel.Warning, Message = "Token is null or empty")]
    public static partial void LogInvalidToken(ILogger logger);

    [LoggerMessage(EventId = 3434, Level = LogLevel.Warning, Message = "JWT Security Key is not configured")]
    public static partial void LogJwtSecurityKeyNotConfigured(ILogger logger);

    [LoggerMessage(EventId = 3435, Level = LogLevel.Warning, Message = "Invalid JWT algorithm or token type")]
    public static partial void LogInvalidJwtAlgorithm(ILogger logger);

    [LoggerMessage(EventId = 3436, Level = LogLevel.Error, Message = "Security token exception while parsing expired token")]
    public static partial void LogSecurityTokenException(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3437, Level = LogLevel.Error, Message = "Error parsing expired token")]
    public static partial void LogErrorParsingExpiredToken(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3438, Level = LogLevel.Error, Message = "Token has expired")]
    public static partial void LogTokenExpired(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3439, Level = LogLevel.Error, Message = "Invalid token")]
    public static partial void LogInvalidTokenWithException(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3440, Level = LogLevel.Error, Message = "Unexpected error during token validation")]
    public static partial void LogUnexpectedTokenValidationError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3463, Level = LogLevel.Error, Message = "Failed to generate access token for user: {UserId}")]
    public static partial void LogFailedToGenerateAccessToken(ILogger logger, string userId);

    [LoggerMessage(EventId = 3464, Level = LogLevel.Error, Message = "Failed to get token expiry for user: {UserId}")]
    public static partial void LogFailedToGetTokenExpiry(ILogger logger, string userId);

    [LoggerMessage(EventId = 3465, Level = LogLevel.Information, Message = "Token refreshed successfully for user: {UserId}")]
    public static partial void LogTokenRefreshed(ILogger logger, string userId);

    [LoggerMessage(EventId = 3466, Level = LogLevel.Error, Message = "Error refreshing token. AccessTokenLength: {AccessTokenLength}, HasRefreshToken: {HasRefreshToken}")]
    public static partial void LogTokenRefreshError(ILogger logger, Exception ex, int accessTokenLength, bool hasRefreshToken);

    [LoggerMessage(EventId = 3441, Level = LogLevel.Warning, Message = "AppUser creation failed for {Email}: {Error}")]
    public static partial void LogAppUserCreationWarning(ILogger logger, string email, string error);

    [LoggerMessage(EventId = 3442, Level = LogLevel.Information, Message = "AppUser {UserId} created successfully for {Email}")]
    public static partial void LogAppUserCreated(ILogger logger, string userId, string email);

    [LoggerMessage(EventId = 3443, Level = LogLevel.Error, Message = "AppUser creation failed for {Email}. Attempting Identity rollback.")]
    public static partial void LogAppUserCreationFailed(ILogger logger, string email, Exception ex);

    [LoggerMessage(EventId = 3444, Level = LogLevel.Information, Message = "Identity rollback succeeded for {Email}")]
    public static partial void LogIdentityRollbackSucceeded(ILogger logger, string email);

    [LoggerMessage(EventId = 3445, Level = LogLevel.Critical, Message = "MANUAL CLEANUP REQUIRED — orphaned Identity user {UserId} for {Email}")]
    public static partial void LogIdentityRollbackCritical(ILogger logger, string userId, string email);

    [LoggerMessage(EventId = 3446, Level = LogLevel.Error, Message = "Error retrieving current user")]
    public static partial void LogGetCurrentUserError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3447, Level = LogLevel.Error, Message = "Error getting OTP status for user: {UserId}")]
    public static partial void LogGetOtpStatusError(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3480, Level = LogLevel.Error, Message = "Error disabling TOTP for user: {UserId}")]
    public static partial void LogTotpDisableError(ILogger logger, string userId, Exception ex);


    [LoggerMessage(EventId = 3449, Level = LogLevel.Error, Message = "Failed to create user session for user {UserId}")]
    public static partial void LogFailedToCreateUserSession(ILogger logger, string userId);

    [LoggerMessage(EventId = 3450, Level = LogLevel.Error, Message = "Failed to get user claims for user: {UserId}")]
    public static partial void LogFailedToGetUserClaims(ILogger logger, string userId);

    [LoggerMessage(EventId = 3451, Level = LogLevel.Error, Message = "Error during login for User: {UserName}")]
    public static partial void LogLoginError(ILogger logger, string userName, Exception ex);

    [LoggerMessage(EventId = 3453, Level = LogLevel.Error, Message = "Duplicate AppUser tracking detected. UserId={UserId} UserName={UserName} RemoteIp={RemoteIp} UserAgent={UserAgent}")]
    public static partial void LogDuplicateAppUserTracking(ILogger logger, Exception ex, string? userId, string userName, string remoteIp, string userAgent);

    [LoggerMessage(EventId = 3452, Level = LogLevel.Information, Message = "User {UserId} signed out")]
    public static partial void LogUserSignedOut(ILogger logger, string userId);

    [LoggerMessage(EventId = 3490, Level = LogLevel.Warning, Message = "Invalid email OTP for user {UserId}")]
    public static partial void LogInvalidEmailOtp(ILogger logger, string userId);

    [LoggerMessage(EventId = 3454, Level = LogLevel.Information, Message = "Email confirmed via OTP for user {UserId}")]
    public static partial void LogEmailConfirmedViaOtp(ILogger logger, string userId);

    [LoggerMessage(EventId = 3455, Level = LogLevel.Error, Message = "Error verifying password for user or email.")]
    public static partial void LogErrorVerifyingPassword(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3456, Level = LogLevel.Warning, Message = "2FA verification attempt for non-existent user: {UserId}")]
    public static partial void Log2FAVerificationAttemptNonExistentUser(ILogger logger, string userId);

    [LoggerMessage(EventId = 3457, Level = LogLevel.Information, Message = "Using temp secret for OTP setup for user: {UserId}")]
    public static partial void LogUsingTempSecret(ILogger logger, string userId);

    [LoggerMessage(EventId = 3458, Level = LogLevel.Information, Message = "OTP enabled for user: {UserId}")]
    public static partial void LogOtpEnabled(ILogger logger, string userId);

    [LoggerMessage(EventId = 3459, Level = LogLevel.Warning, Message = "Invalid OTP code for user: {UserId}")]
    public static partial void LogInvalidOtpCode(ILogger logger, string userId);

    [LoggerMessage(EventId = 3460, Level = LogLevel.Information, Message = "Email sent successfully to {To}")]
    public static partial void LogEmailSent(ILogger logger, string to);

    [LoggerMessage(EventId = 3461, Level = LogLevel.Error, Message = "Failed to send email to {To}: {Errors}")]
    public static partial void LogFailedToSendEmail(ILogger logger, string to, string errors);

    [LoggerMessage(EventId = 3462, Level = LogLevel.Error, Message = "Error sending email to {To}")]
    public static partial void LogErrorSendingEmail(ILogger logger, string to, Exception ex);

    [LoggerMessage(EventId = 3467, Level = LogLevel.Error, Message = "Cache {Operation} failed for key {Key}")]
    public static partial void LogCacheOperationError(ILogger logger, string operation, string key, Exception ex);

    [LoggerMessage(EventId = 3468, Level = LogLevel.Error, Message = "Failed to enqueue background job {RequestType}")]
    public static partial void LogBackgroundJobEnqueueError(ILogger logger, string requestType, Exception ex);


    [LoggerMessage(EventId = 3470, Level = LogLevel.Warning, Message = "JWT authentication failed: {Reason}")]
    public static partial void LogJwtAuthenticationFailed(ILogger logger, string reason, Exception ex);

    [LoggerMessage(EventId = 3471, Level = LogLevel.Error, Message = "Error getting user claims for user {UserId}")]
    public static partial void LogGetUserClaimsError(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3472, Level = LogLevel.Error, Message = "Development identity seed failed")]
    public static partial void LogDevelopmentIdentitySeedError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3473, Level = LogLevel.Information, Message = "Development admin user is available: {Email}")]
    public static partial void LogDevelopmentAdminSeeded(ILogger logger, string email);

    [LoggerMessage(EventId = 3474, Level = LogLevel.Error, Message = "Error deactivating user {UserId}")]
    public static partial void LogDeactivateUserError(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3475, Level = LogLevel.Error, Message = "Error creating role {RoleName}")]
    public static partial void LogRoleCreateError(ILogger logger, string roleName, Exception ex);

    [LoggerMessage(EventId = 3476, Level = LogLevel.Error, Message = "Error updating user {UserId}")]
    public static partial void LogUpdateUserError(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3477, Level = LogLevel.Error, Message = "Error updating role {RoleId}")]
    public static partial void LogRoleUpdateError(ILogger logger, string roleId, Exception ex);

    [LoggerMessage(EventId = 3478, Level = LogLevel.Error, Message = "Error revoking user device {DeviceId}")]
    public static partial void LogUserDeviceRevokeError(ILogger logger, string deviceId, Exception ex);

    [LoggerMessage(EventId = 3479, Level = LogLevel.Error, Message = "Error deleting role {RoleId}")]
    public static partial void LogRoleDeleteError(ILogger logger, string roleId, Exception ex);

    [LoggerMessage(EventId = 3491, Level = LogLevel.Error, Message = "Error updating profile picture for user {UserId}")]
    public static partial void LogProfilePictureUpdateError(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3492, Level = LogLevel.Warning, Message = "Profile picture storage is unavailable while reading the image for user {UserId}")]
    public static partial void LogProfilePictureReadUnavailable(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 3481, Level = LogLevel.Warning, Message = "Session validation concurrency conflict occurred for session {SessionId}. Checking database status.")]
    public static partial void LogSessionConcurrencyConflict(ILogger logger, string sessionId, Exception ex);

    [LoggerMessage(EventId = 3482, Level = LogLevel.Warning, Message = "Payment webhook verification failed for provider {Provider}: {Reason}")]
    public static partial void LogPaymentWebhookVerificationFailed(ILogger logger, string provider, string reason);

    [LoggerMessage(EventId = 3483, Level = LogLevel.Error, Message = "Payment webhook payload processing failed for provider {Provider}")]
    public static partial void LogPaymentWebhookPayloadError(ILogger logger, string provider, Exception ex);

    [LoggerMessage(EventId = 3484, Level = LogLevel.Information, Message = "Payment webhook verified for provider {Provider}. Event {EventId} type {EventType}")]
    public static partial void LogPaymentWebhookVerified(ILogger logger, string provider, string eventId, string eventType);

    [LoggerMessage(EventId = 3486, Level = LogLevel.Warning, Message = "Failed to read setting from cache.")]
    public static partial void LogReadOrgSettingCacheWarning(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3487, Level = LogLevel.Error, Message = "Failed to read tenant setting from database.")]
    public static partial void LogReadOrgSettingDatabaseError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3488, Level = LogLevel.Warning, Message = "Failed to write setting to cache.")]
    public static partial void LogWriteOrgSettingCacheWarning(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3489, Level = LogLevel.Error, Message = "Failed to deserialize setting {Key} to type {Type}")]
    public static partial void LogDeserializeOrgSettingError(ILogger logger, string key, string type, Exception ex);
}
