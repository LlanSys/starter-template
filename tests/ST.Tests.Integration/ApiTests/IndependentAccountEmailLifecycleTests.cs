using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ApiTests;

[Collection("Starter API")]
public sealed class IndependentAccountEmailLifecycleTests(StarterTemplateWebApplicationFactory factory)
{
    [Fact]
    public async Task InvitationRecoveryLogoutAndDisablePreserveTheRealAuthenticationBoundary()
    {
        Guid tenant = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenant);
        using var admin = factory.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
        admin.DefaultRequestHeaders.Add("X-Test-TenantId", tenant.ToString());
        admin.DefaultRequestHeaders.Add("X-Test-Permissions", "users.create,users.edit,users.deactivate");
        using var anonymous = factory.CreateClient(new() { HandleCookies = false });
        anonymous.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.ToString());
        using var mail = factory.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
        mail.BaseAddress = factory.MailpitApi;
        string name = $"invited-{Guid.CreateVersion7():N}";
        string email = name + "@example.test";
        const string password = "InvitedPassword42!";
        using var create = await admin.PostAsJsonAsync("/api/v1/iam/admin/users",
            new CreateAppUserRequest(name, email, "InitialPassword42!", "Neutral", "Account", null, []));
        create.EnsureSuccessStatusCode();
        var account = await create.Content.ReadFromJsonAsync<AppResponse<AppUserResponse>>();
        string id = account!.Data!.Id;
        using var activate = await admin.PatchAsync($"/api/v1/iam/admin/users/{id}/activate", null);
        activate.EnsureSuccessStatusCode();
        using var invitation = await admin.PostAsync($"/api/v1/iam/admin/users/{id}/invite", null);
        invitation.EnsureSuccessStatusCode();
        string body = await ReadEmailAsync(mail, "Your account invitation", email);
        Assert.DoesNotContain("InitialPassword42!", body, StringComparison.Ordinal);
        var link = new Uri(WebUtility.HtmlDecode(Regex.Match(body, "href=\"([^\"]+)\"").Groups[1].Value));
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(link.Query);
        var setup = new AcceptAccountInvitationRequest(id, query["token"].ToString(), password, password);
        using var accepted = await anonymous.PostAsJsonAsync("/api/v1/iam/auth/invitation/accept", setup);
        accepted.EnsureSuccessStatusCode();
        Assert.True((await accepted.Content.ReadFromJsonAsync<AppResponse<bool>>())!.IsSuccess);
        using var replay = await anonymous.PostAsJsonAsync("/api/v1/iam/auth/invitation/accept", setup);
        Assert.False((await replay.Content.ReadFromJsonAsync<AppResponse<bool>>())!.IsSuccess);
        var login = await LoginAsync(anonymous, name, password);
        Assert.True(login.IsAuthenticated);
        Assert.Equal(tenant, login.UserInfo!.TenantId);
        using var signedIn = factory.CreateClient(new() { HandleCookies = false });
        signedIn.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        using var current = await signedIn.GetAsync("/api/v1/iam/auth/me");
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        using var denied = await signedIn.GetAsync("/api/v1/iam/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var logout = await signedIn.PostAsync("/api/v1/iam/auth/logout", null);
        logout.EnsureSuccessStatusCode();
        using var stale = await signedIn.GetAsync("/api/v1/iam/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, stale.StatusCode);
        using var refresh = await anonymous.PostAsJsonAsync("/api/v1/iam/auth/refresh-token", new RefreshTokenRequest(login.Token, login.RefreshToken));
        Assert.False((await refresh.Content.ReadFromJsonAsync<AppResponse<RefreshTokenResponse>>())!.IsSuccess);

        login = await LoginAsync(anonymous, name, password);
        using var forgot = await anonymous.PostAsJsonAsync("/api/v1/iam/auth/password/forgot", new ForgotPasswordRequest(email));
        forgot.EnsureSuccessStatusCode();
        string recovery = await ReadEmailAsync(mail, "Password reset code", email);
        string code = Regex.Match(recovery, @"<strong>(\d{6})</strong>").Groups[1].Value;
        Assert.Equal(6, code.Length);
        using var verified = await anonymous.PostAsJsonAsync("/api/v1/iam/auth/password/reset/verify-otp", new VerifyPasswordResetOtpRequest(email, code));
        verified.EnsureSuccessStatusCode();
        var resetToken = await verified.Content.ReadFromJsonAsync<AppResponse<PasswordResetOtpVerificationResponse>>();
        Assert.True(resetToken!.IsSuccess);
        const string recovered = "RecoveredPassword84!";
        using var reset = await anonymous.PostAsJsonAsync("/api/v1/iam/auth/password/reset",
            new ResetPasswordRequest(email, recovered, null, recovered, resetToken.Data!.TransitionToken));
        reset.EnsureSuccessStatusCode();
        Assert.True((await reset.Content.ReadFromJsonAsync<AppResponse<bool>>())!.IsSuccess);
        signedIn.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        using var resetStale = await signedIn.GetAsync("/api/v1/iam/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, resetStale.StatusCode);
        login = await LoginAsync(anonymous, name, recovered);
        signedIn.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        using var enrollment = await signedIn.PostAsync("/api/v1/iam/users/totp/setup", null);
        enrollment.EnsureSuccessStatusCode();
        var enrollmentResult = await enrollment.Content.ReadFromJsonAsync<AppResponse<TwoFactorSetupInfo>>();
        Assert.True(enrollmentResult!.IsSuccess, enrollmentResult.Message);
        var authenticator = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(enrollmentResult.Data!.ManualEntryKey));
        using var enrolled = await anonymous.PostAsJsonAsync("/api/v1/iam/users/totp/verify",
            new VerifyOtpRequest { UserId = id, Code = authenticator.ComputeTotp(), DeviceFingerprint = "certification" });
        enrolled.EnsureSuccessStatusCode();
        var enrolledResult = await enrolled.Content.ReadFromJsonAsync<AppResponse<VerifyOtpResponse>>();
        Assert.True(enrolledResult!.IsSuccess, enrolledResult.Message);
        Assert.True(enrolledResult.Data!.IsAuthenticated);
        var mfaLogin = await LoginAsync(anonymous, name, recovered);
        Assert.False(mfaLogin.IsAuthenticated);
        using var verifiedMfa = await anonymous.PostAsJsonAsync("/api/v1/iam/users/totp/verify",
            new VerifyOtpRequest { UserId = id, Code = authenticator.ComputeTotp(), DeviceFingerprint = "certification" });
        verifiedMfa.EnsureSuccessStatusCode();
        var verifiedMfaResult = await verifiedMfa.Content.ReadFromJsonAsync<AppResponse<VerifyOtpResponse>>();
        Assert.True(verifiedMfaResult!.IsSuccess, verifiedMfaResult.Message);
        login = login with { Token = verifiedMfaResult.Data!.Token, RefreshToken = verifiedMfaResult.Data.RefreshToken };
        using var deactivateContent = JsonContent.Create(new DeactivateUserRequest("Certification"));
        using var disabled = await admin.PatchAsync($"/api/v1/iam/admin/users/{id}/deactivate", deactivateContent);
        disabled.EnsureSuccessStatusCode();
        signedIn.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        using var disabledSession = await signedIn.GetAsync("/api/v1/iam/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, disabledSession.StatusCode);
        using var disabledLogin = await anonymous.PostAsJsonAsync("/api/v1/iam/auth/login", new { UserName = name, Password = recovered, RememberMe = false, DeviceFingerprint = "certification" });
        Assert.False((await disabledLogin.Content.ReadFromJsonAsync<AppResponse<LoginResponse>>())!.IsSuccess);
        using var disabledMfa = await anonymous.PostAsJsonAsync("/api/v1/iam/users/totp/verify",
            new VerifyOtpRequest { UserId = id, Code = authenticator.ComputeTotp() });
        Assert.False((await disabledMfa.Content.ReadFromJsonAsync<AppResponse<VerifyOtpResponse>>())!.IsSuccess);
        using var disabledRefresh = await anonymous.PostAsJsonAsync("/api/v1/iam/auth/refresh-token", new RefreshTokenRequest(login.Token, login.RefreshToken));
        Assert.False((await disabledRefresh.Content.ReadFromJsonAsync<AppResponse<RefreshTokenResponse>>())!.IsSuccess);
    }

    private static async Task<LoginResponse> LoginAsync(HttpClient client, string name, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/iam/auth/login",
            new { UserName = name, Password = password, RememberMe = false, DeviceFingerprint = "certification" });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AppResponse<LoginResponse>>();
        Assert.True(result!.IsSuccess, result.Message);
        return result.Data!;
    }

    private static async Task<string> ReadEmailAsync(HttpClient client, string subject, string email)
    {
        using var summary = await client.GetFromJsonAsync<JsonDocument>("/api/v1/messages");
        var message = summary!.RootElement.GetProperty("messages").EnumerateArray().Single(message =>
            message.GetProperty("Subject").GetString() == subject && message.GetProperty("To").EnumerateArray()
                .Any(recipient => recipient.GetProperty("Address").GetString() == email));
        using var content = await client.GetFromJsonAsync<JsonDocument>("/api/v1/message/" + message.GetProperty("ID").GetString());
        return content!.RootElement.GetProperty("HTML").GetString()!;
    }
}
