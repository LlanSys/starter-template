using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ST.Application.Features.ControlPlane.Demo;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Menus.Entities;
using ST.Domain.Features.IAM.Permissions.Entities;
using ST.Domain.Features.IAM.ReferenceData.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Entities;
using ST.Infrastructure.Contracts.Implementations.Common;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Demo;
using ST.SharedKernel.Features.IAM.Users.Dtos;
namespace ST.Infrastructure.Features.ControlPlane.Demo;

internal sealed class EnterDemoHandler(IControlPlaneUnitOfWork controlPlane, IHttpContextAccessor http,
    IServiceScopeFactory scopes, UserManager<AppUser> platformUsers, ST.Application.Features.ControlPlane.Tenants.Contracts.ITenantWorkspaceProvisioner provisioner) : IRequestHandler<EnterDemoCommand, AppResponse<LoginResponse>>
{
    public async Task<AppResponse<LoginResponse>> Handle(EnterDemoCommand request, CancellationToken cancellationToken)
    {
        var principal = http.HttpContext?.User;
        if (principal is null || !PlatformAccess.IsStaff(principal)) return AppResponses.Failure<LoginResponse>("Platform administrator access is required.");
        var actor = await platformUsers.FindByIdAsync(request.ActorId).ConfigureAwait(false);
        if (actor is null || !actor.IsActive || actor.IsDeleted || !actor.TwoFactorEnabled)
            return AppResponses.Failure<LoginResponse>("Complete platform administrator MFA before entering the demo.");
        var activeModules = await controlPlane.Modules.ListAsync(query => query.Where(module => module.IsActive && !module.IsRetired), cancellationToken).ConfigureAwait(false);
        var ready = activeModules.Where(module => module.Key.Equals("Core", StringComparison.OrdinalIgnoreCase)
            || module.Key.Equals("IAM", StringComparison.OrdinalIgnoreCase) || module.Key.Equals("Showcase", StringComparison.OrdinalIgnoreCase) || module.Key.Equals("Payments", StringComparison.OrdinalIgnoreCase)).ToArray();
        var demo = await controlPlane.Tenants.FindAll().AsTracking().Include(tenant => tenant.Modules).FirstOrDefaultAsync(tenant => tenant.Id == DemoWorkspace.TenantId, cancellationToken).ConfigureAwait(false);
        if (demo is null)
        {
            if (await controlPlane.Tenants.AnyAsync(tenant => tenant.Identifier == DemoWorkspace.Identifier, cancellationToken).ConfigureAwait(false))
                return AppResponses.Failure<LoginResponse>("The demo identifier conflicts with an existing workspace.");
            var source = await controlPlane.Tenants.FindByIdAsync(actor.TenantId, cancellationToken).ConfigureAwait(false);
            if (source is null) return AppResponses.Failure<LoginResponse>("The platform workspace is not registered.");
            demo = new Tenant { Id = DemoWorkspace.TenantId, Identifier = DemoWorkspace.Identifier, DisplayName = "Vendor demo",
                HostName = "vendor-demo.invalid", IsDemo = true, DeploymentStampId = source.DeploymentStampId,
                MaxUsers = 100, CreatedBy = request.ActorId };
            demo.Approve(request.ActorId);
            demo.MarkAsActive();
            await controlPlane.Tenants.CreateAsync(demo, cancellationToken).ConfigureAwait(false);
            foreach (var module in ready) demo.Modules.Add(new TenantModule { TenantId = demo.Id, ModuleKey = module.Key, CreatedBy = request.ActorId });
            await controlPlane.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }
        if (!demo.IsDemo || demo.Identifier != DemoWorkspace.Identifier || demo.Status != ST.Domain.Features.ControlPlane.Tenants.Enums.TenantStatus.Active)
            return AppResponses.Failure<LoginResponse>("The demo workspace is unavailable.");

        foreach (var module in ready)
        {
            var assigned = demo.Modules.FirstOrDefault(item => string.Equals(item.ModuleKey, module.Key, StringComparison.OrdinalIgnoreCase));
            if (assigned is null) await controlPlane.TenantModules.CreateAsync(new TenantModule { TenantId = demo.Id, ModuleKey = module.Key, CreatedBy = request.ActorId }, cancellationToken).ConfigureAwait(false);
            else { assigned.IsActive = true; assigned.ExpiresAt = null; assigned.UpdatedBy = request.ActorId; assigned.UpdatedAt = DateTimeOffset.UtcNow; }
        }
        var flags = await controlPlane.FeatureFlags.ListAsync(q=>q.Where(x=>x.ScopeTenantId==Guid.Empty || x.ScopeTenantId==demo.Id),cancellationToken).ConfigureAwait(false);
        foreach(var flag in flags.Where(x=>x.ScopeTenantId==Guid.Empty && ready.Any(m=>string.Equals(m.Key,x.ModuleKey,StringComparison.OrdinalIgnoreCase)))) {
          var change=flags.FirstOrDefault(x=>x.ScopeTenantId==demo.Id && x.ModuleKey==flag.ModuleKey && x.Key==flag.Key);
          if(change is null) await controlPlane.FeatureFlags.CreateAsync(new ST.Domain.Features.ControlPlane.Modules.Entities.ModuleFeatureFlag { ScopeTenantId=demo.Id,ModuleKey=flag.ModuleKey,Key=flag.Key,Name=flag.Name,IsEnabled=true,CreatedBy=request.ActorId },cancellationToken).ConfigureAwait(false);
          else { change.IsEnabled=true;await controlPlane.FeatureFlags.UpdateAsync(change,cancellationToken).ConfigureAwait(false); }
        }
        demo.DemoFixtureVersion = Math.Max(1,demo.DemoFixtureVersion);
        await controlPlane.CompleteAsync(cancellationToken).ConfigureAwait(false);
        await using var scope = scopes.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<WorkspaceTenantOverride>().TenantId = demo.Id;
        var context = scope.ServiceProvider.GetRequiredService<IamDBContext>();
        var prepared = await provisioner.InitializeAsync(demo.Id, null, request.ActorId, cancellationToken).ConfigureAwait(false);
        if (!prepared.IsSuccess) return AppResponses.Failure<LoginResponse>(prepared.Message ?? "Could not prepare demo.");
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var name = $"demo-{(request.ReadOnly ? "viewer" : "admin")}-{actor.Id}";
        var user = await users.FindByNameAsync(name).ConfigureAwait(false);
        if (user is null)
        {
            user = AppUser.CreateSystemUser(demo.Id, name, $"{name}@example.invalid", request.ActorId);
            user.FirstName = actor.FirstName; user.LastName = "(Demo)"; user.IsActive = true; user.EmailConfirmed = true;
            Ensure(await users.CreateAsync(user).ConfigureAwait(false));
        }
        var access = await scope.ServiceProvider.GetRequiredService<ST.Application.Features.ControlPlane.Modules.IEffectiveModulePolicy>().GetAsync(cancellationToken).ConfigureAwait(false);
        var keys = access.Catalogue.Where(entry => entry.Kind == ST.Domain.Features.ControlPlane.Modules.Entities.ModuleCatalogueKind.Permission && access.EntryAvailable(entry)).Select(entry => entry.Key).ToList();
        var existingClaims = await users.GetClaimsAsync(user).ConfigureAwait(false);
        if (request.ReadOnly) keys = keys.Where(key => key.EndsWith(".view", StringComparison.OrdinalIgnoreCase) || key.EndsWith(".read", StringComparison.OrdinalIgnoreCase)).ToList();
        var desiredClaims = keys.Select(key => new Claim("permission", key)).Append(new Claim("demo_workspace", "true")).Append(new Claim("demo_actor_id", actor.Id)).Append(new Claim("demo_readonly", request.ReadOnly ? "true" : "false"));
        var missing = desiredClaims.Where(claim => !existingClaims.Any(existing => existing.Type == claim.Type && existing.Value == claim.Value)).ToArray();
        if (missing.Length > 0) Ensure(await users.AddClaimsAsync(user, missing).ConfigureAwait(false));
        if (user.TenantId != demo.Id || !user.IsActive || user.IsDeleted) return AppResponses.Failure<LoginResponse>("The demo account is unavailable.");
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var roleName = request.ReadOnly ? "Demo Viewer" : "Tenant Administrator";
        if (!await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
            Ensure(await roleManager.CreateAsync(new AppRole { Id = Guid.CreateVersion7().ToString(), TenantId = demo.Id, Name = roleName }).ConfigureAwait(false));
        if (!await users.IsInRoleAsync(user, roleName).ConfigureAwait(false))
            Ensure(await users.AddToRoleAsync(user, roleName).ConfigureAwait(false));
        var sessions = scope.ServiceProvider.GetRequiredService<ISessionService>();
        var session = await sessions.CreateSessionAsync(user.Id, Guid.CreateVersion7(), http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown", "Vendor demo", cancellationToken).ConfigureAwait(false);
        if (!session.IsSuccess || session.Data == Guid.Empty) return AppResponses.Failure<LoginResponse>("Could not establish the demo session.");
        var claimsService = scope.ServiceProvider.GetRequiredService<IClaimsService>();
        var claims = await claimsService.GetUserClaimsAsync(user, session.Data).ConfigureAwait(false);
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
        var token = await jwt.CreateTokenAsync(claims).ConfigureAwait(false);
        var refresh = jwt.CreateRefreshToken();
        var unit = scope.ServiceProvider.GetRequiredService<IIamUnitOfWork>();
        await unit.TokenRepository.AddRefreshTokenAsync(RefreshToken.Create(user.Id, refresh, DateTimeOffset.UtcNow.AddHours(1), request.ActorId, null)).ConfigureAwait(false);
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(new LoginResponse(user.Id, user.FirstName, user.LastName, user.Email!, false, false, true,
            token, refresh, session.Data.ToString(), jwt.GetTokenExpiry(token), user.ToAppUserResponse(), null));
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException("Demo identity provisioning failed.");
    }
}
