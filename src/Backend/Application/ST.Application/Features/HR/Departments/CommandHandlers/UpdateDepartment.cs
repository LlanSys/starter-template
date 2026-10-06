using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.HR.Departments.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Departments.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Departments.CommandHandlers;



internal sealed class UpdateDepartmentCommandHandler(IHrUnitOfWork unitOfWork, ILogger<UpdateDepartmentCommandHandler> logger)
    : IRequestHandler<UpdateDepartmentCommand, AppResponse<DepartmentResponse>>
{
    public async Task<AppResponse<DepartmentResponse>> Handle(UpdateDepartmentCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var request = command.Request;
            var department = await unitOfWork.DepartmentRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
            if (department is null)
            {
                return AppResponses.Failure<DepartmentResponse>($"Department {command.Id} not found.");
            }

            var code = request.Code.Trim().ToUpperInvariant();
            var duplicate = await unitOfWork.DepartmentRepository
                .AnyAsync(existing => existing.Id != command.Id && existing.Code == code, cancellationToken)
                .ConfigureAwait(false);

            if (duplicate)
            {
                return AppResponses.Failure<DepartmentResponse>($"Department code {code} is already in use.");
            }

            department.Update(code, request.Name, request.Description, request.IsActive, command.UserId);
            await unitOfWork.DepartmentRepository.UpdateAsync(department, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved
                ? AppResponses.Success("Department updated.", department.ToDepartmentResponse())
                : AppResponses.Failure<DepartmentResponse>("Department update failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogStaffMembersFetchFailed(logger, ex);
            throw;
        }
    }
}
