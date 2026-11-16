using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using ST.Application.Features.ControlPlane.Tenants.Commands;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Configuration;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants;
using ST.SharedKernel.Features.Shared.Common.Enums;
namespace ST.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

internal sealed class SaveTenantFrontendAddressHandler(IControlPlaneUnitOfWork unit, IOptions<OrgSettings> settings,
    ICurrentActorProvider actor, TimeProvider clock) : IRequestHandler<SaveTenantFrontendAddressCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(SaveTenantFrontendAddressCommand request, CancellationToken ct)
    {
        var tenant = await unit.Tenants.FindByIdAsync(request.Id, ct).ConfigureAwait(false);

        if (tenant is null)
        {
            return AppResponses.NotFound<bool>("Tenant not found.");
        }

        if (!string.Equals(Convert.ToBase64String(tenant.RowVersion), request.Request.Revision, StringComparison.Ordinal))
        {
            return Conflict("This tenant changed. Refresh and try again.");
        }
        string? origin = null;

        if (request.Request.Origin is not null && !FrontendOrigin.TryNormalize(request.Request.Origin, out origin))
        {
            return AppResponses.Failure<bool>("Enter a valid app origin.");
        }

        if (origin is not null && settings.Value.CentralFrontendOrigins.Any(value => FrontendOrigin.TryNormalize(value, out var shared) && shared == origin))
        {
            return AppResponses.Failure<bool>("This is a shared portal address. Enter a distinct company app address.");
        }

        if (origin is not null && await unit.Tenants.AnyAsync(other => other.Id != tenant.Id && other.DedicatedFrontendOrigin == origin, ct).ConfigureAwait(false))
        {
            return Conflict("This app address is already assigned to another organization.");
        }
        tenant.DedicatedFrontendOrigin = origin;
        tenant.UpdatedBy = actor.ActorId;
        tenant.UpdatedAt = clock.GetUtcNow();
        await unit.Tenants.UpdateAsync(tenant, ct).ConfigureAwait(false);

        try
        {
            await unit.CompleteAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent tenant edit invalidates this operator's revision.
            return Conflict("This tenant changed. Refresh and try again.");
        }
        catch (DbUpdateException exception) when (IsAddressConflict(exception))
        {
            // The unique index fences concurrent registrations; translate only this expected conflict.
            return Conflict("This app address was just assigned. Refresh and try again.");
        }
        return AppResponses.Success("Company app address saved.", true);
    }

    private static AppResponse<bool> Conflict(string message)
    {
        return AppResponses.Failure<bool>(new AppError(ErrorType.Conflict, "CONFLICT", message));
    }

    private static bool IsAddressConflict(DbUpdateException exception)
    {
        const string index = "UX_Tenants_DedicatedFrontendOrigin";
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres && postgres.ConstraintName == index
            || exception.InnerException is SqlException { Number: 2601 or 2627 } sql && sql.Message.Contains(index, StringComparison.Ordinal);
    }
}
