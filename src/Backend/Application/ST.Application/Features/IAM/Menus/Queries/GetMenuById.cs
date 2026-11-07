using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
namespace ST.Application.Features.IAM.Menus.Queries;
internal sealed class GetMenuByIdQueryHandler(IEffectiveNavigation navigation) : IRequestHandler<GetMenuByIdQuery, AppResponse<MenuResponse>>
{
    public async Task<AppResponse<MenuResponse>> Handle(GetMenuByIdQuery query, CancellationToken cancellationToken)
    {
        var row = (await navigation.GetAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(x => x.Menu.Id == query.Id && x.Available);
        return row is null ? AppResponses.Failure<MenuResponse>("Navigation item not found.") : AppResponses.Success(row.ToResponse());
    }
}
