using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Menus.Commands;



internal sealed class DeleteMenuCommandHandler(IIamUnitOfWork unitOfWork, ILogger<DeleteMenuCommandHandler> logger, IEffectiveNavigation navigation)
    : IRequestHandler<DeleteMenuCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeleteMenuCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var standard = (await navigation.GetAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(x => x.Menu.Id == command.Id && x.Definition is not null);
            if (standard is not null)
            {
                var item = standard.Menu;
                var overrideResult = await navigation.SaveOverrideAsync(standard.Definition!.Key, new ST.SharedKernel.Features.IAM.Menus.Dtos.UpdateMenuRequest { Id = item.Id, Title = item.Title, Url = item.Url, Icon = item.Icon, Placement = item.Placement, ParentId = item.ParentId, IsActive = false, DisplayOrder = item.DisplayOrder, RequiredRoleNames = item.RequiredRoleNames, RequiredPermissionKeys = item.RequiredPermissionKeys, ExpectedVersion = standard.OverrideVersion }, command.UserId, cancellationToken).ConfigureAwait(false);
                return overrideResult.IsSuccess ? AppResponses.Success(true) : AppResponses.Failure<bool>(overrideResult.Message);
            }
            var menu = await unitOfWork.MenuRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
            if (menu is null)
            {
                return AppResponses.Failure<bool>($"Menu {command.Id} not found.");
            }

            var hasChildren = await unitOfWork.MenuRepository
                .AnyAsync(child => child.ParentId == command.Id, cancellationToken)
                .ConfigureAwait(false);

            if (hasChildren)
            {
                return AppResponses.Failure<bool>("This menu has child menu items. Move or delete the children first.");
            }

            menu.MarkAsDeleted(command.UserId);
            await unitOfWork.MenuRepository.UpdateAsync(menu, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved ? AppResponses.Success("Menu deleted.", true) : AppResponses.Failure<bool>("Menu delete failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(DeleteMenuCommandHandler), ex);
            throw;
        }
    }
}
