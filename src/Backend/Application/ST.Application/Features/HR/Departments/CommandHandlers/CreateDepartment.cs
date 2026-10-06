using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.HR.Departments.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.HR.Departments.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Departments.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Departments.CommandHandlers;



internal sealed class CreateDepartmentCommandHandler(IHrUnitOfWork unitOfWork, ILogger<CreateDepartmentCommandHandler> logger)
    : IRequestHandler<CreateDepartmentCommand, AppResponse<DepartmentResponse>>
{
    public async Task<AppResponse<DepartmentResponse>> Handle(CreateDepartmentCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var request = command.Request;
            var code = request.Code.Trim().ToUpperInvariant();
            var duplicate = await unitOfWork.DepartmentRepository
                .AnyAsync(department => department.Code == code, cancellationToken)
                .ConfigureAwait(false);

            if (duplicate)
            {
                return AppResponses.Failure<DepartmentResponse>($"Department code {code} is already in use.");
            }

            var department = Department.Create(code, request.Name, request.Description, command.UserId);
            await unitOfWork.DepartmentRepository.CreateAsync(department, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved
                ? AppResponses.Success("Department created.", department.ToDepartmentResponse())
                : AppResponses.Failure<DepartmentResponse>("Department create failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogStaffMembersFetchFailed(logger, ex);
            throw;
        }
    }
}
