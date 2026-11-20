using MediatR;
using ST.Domain.Features.Shared.Contracts;
using ST.Domain.Features.Shared.OrgSettings.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Common.Enums;
using ST.SharedKernel.Features.Shared.OrgSettings;

namespace ST.Application.Features.Shared.OrgSettings.SaveCatalogSetting;

internal sealed class SaveCatalogOrgSettingHandler(ISharedUnitOfWork unit, TimeProvider clock)
    : IRequestHandler<SaveCatalogOrgSettingCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(SaveCatalogOrgSettingCommand request, CancellationToken cancellationToken)
    {
        var definition = OrganizationSettingCatalog.Find(request.Key);

        if (definition is null || (!request.Reset && !OrganizationSettingCatalog.IsValid(request.Key, request.Value)))
        {
            return AppResponses.Failure<bool>("Choose a supported setting and value.");
        }

        return await unit.ExecuteInTransactionAsync(
            () => SaveAsync(request, definition, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    private async Task<AppResponse<bool>> SaveAsync(
        SaveCatalogOrgSettingCommand request,
        OrgSettingDefinition definition,
        CancellationToken cancellationToken)
    {
        await unit.OrgSettingRepository.LockSettingAsync(request.Key, cancellationToken).ConfigureAwait(false);
        var row = await unit.OrgSettingRepository.FirstOrDefaultAsync(
            item => item.Key == request.Key, cancellationToken).ConfigureAwait(false);

        var revision = row is null ? null : Convert.ToBase64String(row.RowVersion);

        if (!string.Equals(revision, request.Revision, StringComparison.Ordinal))
        {
            return AppResponses.Failure<bool>(new AppError(
                ErrorType.Conflict, "CONFLICT", "This setting changed. Refresh the settings and try again."));
        }

        var previous = row?.Value;

        if (request.Reset)
        {
            if (row is null)
            {
                return AppResponses.Success(true);
            }

            await unit.OrgSettingRepository.DeleteAsync(row.Id, cancellationToken).ConfigureAwait(false);
        }
        else if (row is null)
        {
            var setting = new OrgSetting(request.Key, request.Value!, request.ActorId, definition.Description);
            await unit.OrgSettingRepository.CreateAsync(setting, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            row.Value = request.Value!;
            row.Description = definition.Description;
            await unit.OrgSettingRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        }

        var now = clock.GetUtcNow();
        var change = OrgSettingChange.Record(
            request.Key, previous, request.Reset ? null : request.Value, request.Reset, request.ActorId, now);

        await unit.OrgSettingRepository.RecordChangeAsync(change, cancellationToken).ConfigureAwait(false);
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);

        return AppResponses.Success(request.Reset ? "Default restored." : "Setting saved.", true);
    }
}
