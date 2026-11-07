using MediatR;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.SharedKernel.Dtos.Common;
namespace ST.Application.Features.ControlPlane.Modules;
internal sealed class SaveModuleFlagHandler(IControlPlaneUnitOfWork unit) : IRequestHandler<SaveModuleFlagCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(SaveModuleFlagCommand command, CancellationToken cancellationToken)
    {
        var key = command.ModuleKey.Trim().ToLowerInvariant(); var request = command.Request; var flagKey = request.Key.Trim().ToLowerInvariant();
        var modules = await unit.Modules.ListAsync(ct: cancellationToken).ConfigureAwait(false);
        var module = modules.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        if (module is null || module.IsRetired) return AppResponses.Failure<bool>("Module not found or retired.");
        if (module.IsEssential && flagKey == "enabled" && !request.IsEnabled) return AppResponses.Failure<bool>("Essential modules cannot be switched off.");
        if (request.TenantId != Guid.Empty)
        {
            if (!await unit.Tenants.AnyAsync(x => x.Id == request.TenantId, cancellationToken).ConfigureAwait(false)) return AppResponses.Failure<bool>("Tenant not found.");
            if (!await unit.FeatureFlags.AnyAsync(x => x.ModuleKey == key && x.Key == flagKey && x.ScopeTenantId == Guid.Empty, cancellationToken).ConfigureAwait(false)) return AppResponses.Failure<bool>("Define the platform flag before adding a tenant override.");
        }
        var row = await unit.FeatureFlags.FirstOrDefaultAsync(x => x.ModuleKey == key && x.Key == flagKey && x.ScopeTenantId == request.TenantId, cancellationToken).ConfigureAwait(false);
        if (request.UseDefault)
        {
            if (request.TenantId == Guid.Empty) return AppResponses.Failure<bool>("Only tenant overrides can be reset to the platform default.");
            if (row is not null) await unit.FeatureFlags.DeleteAsync(row.Id, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            if (row is null) { row = new ModuleFeatureFlag { ModuleKey = key, Key = flagKey, ScopeTenantId = request.TenantId, Name = request.Name.Trim(), CreatedBy = command.ActorId }; await unit.FeatureFlags.CreateAsync(row, cancellationToken).ConfigureAwait(false); }
            row.Name = request.Name.Trim(); row.IsEnabled = request.IsEnabled; row.UpdatedAt = DateTimeOffset.UtcNow; row.UpdatedBy = command.ActorId;
        }
        if (!request.UseDefault && row is not null) await unit.FeatureFlags.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(true);
    }
}
