using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.HR.Departments.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Departments.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Departments.QueryHandlers;



internal sealed class GetDepartmentsQueryHandler(IHrUnitOfWork unitOfWork, ILogger<GetDepartmentsQueryHandler> logger)
    : IRequestHandler<GetDepartmentsQuery, AppResponse<IReadOnlyList<DepartmentResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<DepartmentResponse>>> Handle(GetDepartmentsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var departments = await unitOfWork.DepartmentRepository
                .ListAsync(
                    departments => departments
                        .Where(static department => department.IsActive)
                        .OrderBy(static department => department.Name),
                    cancellationToken)
                .ConfigureAwait(false);

            return AppResponses.Success<IReadOnlyList<DepartmentResponse>>(
                departments.Select(static department => department.ToDepartmentResponse()).ToList());
        }
        catch (Exception ex)
        {
            LogDefinitions.LogStaffMembersFetchFailed(logger, ex);
            throw;
        }
    }
}
