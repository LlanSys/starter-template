using ST.Application.Features.Shared.Dashboard.QueryHandlers;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Dashboard.Dtos;
using MediatR;

namespace ST.Application.Features.Shared.Dashboard.QueryHandlers;

internal sealed class GetDashboardSummaryQueryHandler(IIamUnitOfWork unitOfWork)
    : IRequestHandler<GetDashboardSummaryQuery, AppResponse<DashboardSummaryResponse>>
{
    public async Task<AppResponse<DashboardSummaryResponse>> Handle(GetDashboardSummaryQuery query, CancellationToken cancellationToken)
    {
        var summary = await unitOfWork.UserRepository.ListAsync(users => users
            .GroupBy(user => 1).Select(group => new DashboardSummaryResponse(
                group.Count(), group.Count(user => user.IsActive),
                group.Count(user => user.IsActive && !user.EmailConfirmed), group.Count(user => !user.IsActive))), cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(summary.FirstOrDefault() ?? new DashboardSummaryResponse(0, 0, 0, 0));
    }
}
