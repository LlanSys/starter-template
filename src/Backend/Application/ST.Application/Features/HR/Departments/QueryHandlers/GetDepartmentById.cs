using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.HR.Departments.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Departments.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Departments.QueryHandlers;



internal sealed class GetDepartmentByIdQueryHandler(IHrUnitOfWork unitOfWork, ILogger<GetDepartmentByIdQueryHandler> logger)
    : IRequestHandler<GetDepartmentByIdQuery, AppResponse<DepartmentResponse>>
{
    public async Task<AppResponse<DepartmentResponse>> Handle(GetDepartmentByIdQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var department = await unitOfWork.DepartmentRepository.FindByIdAsync(query.Id, cancellationToken).ConfigureAwait(false);
            return department is null
                ? AppResponses.Failure<DepartmentResponse>($"Department {query.Id} not found.")
                : AppResponses.Success(department.ToDepartmentResponse());
        }
        catch (Exception ex)
        {
            LogDefinitions.LogStaffMembersFetchFailed(logger, ex);
            throw;
        }
    }
}
