using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Menus.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Menus.Queries;



internal sealed class GetMenuByIdQueryHandler(IIamUnitOfWork unitOfWork, ILogger<GetMenuByIdQueryHandler> logger)
    : IRequestHandler<GetMenuByIdQuery, AppResponse<MenuResponse>>
{
    public async Task<AppResponse<MenuResponse>> Handle(GetMenuByIdQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var menu = await unitOfWork.MenuRepository.FindByIdAsync(query.Id, cancellationToken).ConfigureAwait(false);
            return menu is null
                ? AppResponses.Failure<MenuResponse>($"Menu {query.Id} not found.")
                : AppResponses.Success(menu.ToMenuResponse());
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(GetMenuByIdQueryHandler), ex);
            throw;
        }
    }
}
