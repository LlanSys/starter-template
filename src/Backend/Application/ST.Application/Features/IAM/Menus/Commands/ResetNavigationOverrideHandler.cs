using MediatR;
using ST.SharedKernel.Dtos.Common;
namespace ST.Application.Features.IAM.Menus.Commands;
internal sealed class ResetNavigationOverrideHandler(IEffectiveNavigation navigation) : IRequestHandler<ResetNavigationOverrideCommand, AppResponse<bool>>
{
    public Task<AppResponse<bool>> Handle(ResetNavigationOverrideCommand command, CancellationToken cancellationToken) => navigation.ResetOverrideAsync(command.PageKey, cancellationToken);
}
