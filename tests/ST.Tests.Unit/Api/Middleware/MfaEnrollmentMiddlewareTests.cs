using ST.Api.Middleware;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text;

namespace ST.Tests.Unit.Api.Middleware;

public sealed class MfaEnrollmentMiddlewareTests
{
    private const string UserId = "019e012a-59c7-7abc-bf6a-46e4934515b5";

    [Fact]
    public async Task InvokeAsync_Should_Block_Protected_Api_When_Required_Role_Is_Not_Enrolled()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        using var userManager = CreateUserManager(new AppUser { Id = UserId, UserName = "admin", TwoFactorEnabled = false });
        var context = CreateContext("/api/v1/iam/admin/users", isAuthenticated: true, includeUserId: true, includeRequiredRole: true);

        await middleware.InvokeAsync(context, userManager);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Contains("MFA_ENROLLMENT_REQUIRED", await ReadResponseAsync(context));
    }

    [Fact]
    public async Task InvokeAsync_Should_Allow_Anonymous_Request_When_No_User_Id_Is_Present()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        using var userManager = CreateUserManager();
        var context = CreateContext("/api/v1/iam/auth/forgot-password", isAuthenticated: false, includeUserId: false, includeRequiredRole: false);

        await middleware.InvokeAsync(context, userManager);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Should_Allow_Malformed_Authenticated_Principal_To_Reach_Normal_Authorization()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        using var userManager = CreateUserManager();
        var context = CreateContext("/api/v1/iam/admin/users", isAuthenticated: true, includeUserId: false, includeRequiredRole: true);

        await middleware.InvokeAsync(context, userManager);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Should_Allow_Totp_Setup_When_Required_Role_Is_Not_Enrolled()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        using var userManager = CreateUserManager(new AppUser { Id = UserId, UserName = "admin", TwoFactorEnabled = false });
        var context = CreateContext("/api/v1/iam/users/totp/setup", isAuthenticated: true, includeUserId: true, includeRequiredRole: true);

        await middleware.InvokeAsync(context, userManager);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Should_Allow_Protected_Api_When_Required_Role_Is_Enrolled()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        using var userManager = CreateUserManager(new AppUser { Id = UserId, UserName = "admin", TwoFactorEnabled = true });
        var context = CreateContext("/api/v1/iam/admin/users", isAuthenticated: true, includeUserId: true, includeRequiredRole: true);

        await middleware.InvokeAsync(context, userManager);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private static MfaEnrollmentMiddleware CreateMiddleware(RequestDelegate next)
    {
        var options = Options.Create(new MfaSettings
        {
            Enabled = true,
            EnforceEnrollment = true,
            RequiredRoles = ["System Administrator"]
        });

        return new MfaEnrollmentMiddleware(next, options, NullLogger<MfaEnrollmentMiddleware>.Instance);
    }

    private static DefaultHttpContext CreateContext(
        string path,
        bool isAuthenticated,
        bool includeUserId,
        bool includeRequiredRole)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (isAuthenticated)
        {
            var claims = new List<Claim>();
            if (includeUserId)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, UserId));
            }

            if (includeRequiredRole)
            {
                claims.Add(new Claim(ClaimTypes.Role, "System Administrator"));
            }

            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "unit-test"));
        }

        return context;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The test store is owned and disposed by UserManager in these tests.")]
    private static UserManager<AppUser> CreateUserManager(params AppUser[] users)
    {
        var store = new TestUserStore(users);

        return new UserManager<AppUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<AppUser>>.Instance);
    }

    private static async Task<string> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private sealed class TestUserStore(params AppUser[] users) : IUserStore<AppUser>, IUserTwoFactorStore<AppUser>
    {
        private readonly Dictionary<string, AppUser> _users = users.ToDictionary(static user => user.Id, StringComparer.OrdinalIgnoreCase);

        public Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
            => Task.FromResult(_users.GetValueOrDefault(userId));

        public Task<bool> GetTwoFactorEnabledAsync(AppUser user, CancellationToken cancellationToken)
            => Task.FromResult(user.TwoFactorEnabled);

        public Task SetTwoFactorEnabledAsync(AppUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.TwoFactorEnabled = enabled;
            return Task.CompletedTask;
        }

        public Task<string> GetUserIdAsync(AppUser user, CancellationToken cancellationToken)
            => Task.FromResult(user.Id);

        public Task<string?> GetUserNameAsync(AppUser user, CancellationToken cancellationToken)
            => Task.FromResult(user.UserName);

        public Task SetUserNameAsync(AppUser user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(AppUser user, CancellationToken cancellationToken)
            => Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(AppUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken)
        {
            _users[user.Id] = user;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken)
        {
            _users[user.Id] = user;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken)
        {
            _users.Remove(user.Id);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
            => Task.FromResult(_users.Values.SingleOrDefault(user => user.NormalizedUserName == normalizedUserName));

        public void Dispose()
        {
        }
    }
}
