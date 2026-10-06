using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Employees.CommandHandlers;



internal sealed class DeleteEmployeeCommandHandler(IHrUnitOfWork unitOfWork, ILogger<DeleteEmployeeCommandHandler> logger)
    : IRequestHandler<DeleteEmployeeCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeleteEmployeeCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await unitOfWork.EmployeeRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
            if (employee is null)
            {
                return AppResponses.Failure<bool>($"Employee {command.Id} not found.");
            }

            await unitOfWork.EmployeeRepository.SoftDeleteAsync(command.Id, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved
                ? AppResponses.Success("Employee deleted.", true)
                : AppResponses.Failure<bool>("Failed to delete employee.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogStaffMembersFetchFailed(logger, ex);
            throw;
        }
    }
}
