using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Departments.CommandHandlers;



internal sealed class DeleteDepartmentCommandHandler(IHrUnitOfWork unitOfWork, ILogger<DeleteDepartmentCommandHandler> logger)
    : IRequestHandler<DeleteDepartmentCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeleteDepartmentCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var department = await unitOfWork.DepartmentRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
            if (department is null)
            {
                return AppResponses.Failure<bool>($"Department {command.Id} not found.");
            }

            var hasEmployees = await unitOfWork.EmployeeRepository
                .AnyAsync(employee => employee.DepartmentId == command.Id, cancellationToken)
                .ConfigureAwait(false);

            if (hasEmployees)
            {
                return AppResponses.Failure<bool>("This department has assigned employees. Reassign them before deleting the department.");
            }

            department.MarkAsDeleted(command.UserId);
            await unitOfWork.DepartmentRepository.UpdateAsync(department, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved
                ? AppResponses.Success("Department deleted.", true)
                : AppResponses.Failure<bool>("Department delete failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogStaffMembersFetchFailed(logger, ex);
            throw;
        }
    }
}
