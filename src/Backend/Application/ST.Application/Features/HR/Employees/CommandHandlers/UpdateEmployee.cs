using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Employees.Dtos;
using ST.SharedKernel.Features.Shared.Phone;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Employees.CommandHandlers;



internal sealed class UpdateEmployeeCommandHandler(IHrUnitOfWork unitOfWork, ILogger<UpdateEmployeeCommandHandler> logger)
    : IRequestHandler<UpdateEmployeeCommand, AppResponse<EmployeeResponse>>
{
    public async Task<AppResponse<EmployeeResponse>> Handle(UpdateEmployeeCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var request = command.Request;
            var employee = await unitOfWork.EmployeeRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
            if (employee is null)
            {
                return AppResponses.Failure<EmployeeResponse>($"Employee {command.Id} not found.");
            }

            var duplicateEmail = await unitOfWork.EmployeeRepository
                .AnyAsync(existing => existing.Id != command.Id && existing.Email == request.Email, cancellationToken)
                .ConfigureAwait(false);

            if (duplicateEmail)
            {
                return AppResponses.Failure<EmployeeResponse>($"Employee email {request.Email} is already in use.");
            }

            var phone = PhoneNumberFormatter.Normalize(
                request.CountryCode,
                request.PhoneNationalNumber,
                request.PhoneNumber);

            employee.Update(
                employee.Number,
                request.Email,
                request.FirstName,
                request.LastName,
                request.IdNumber,
                phone.CountryCode,
                phone.NationalNumber,
                phone.E164,
                request.DepartmentId,
                request.ManagerId,
                command.UserId);

            await unitOfWork.EmployeeRepository.UpdateAsync(employee, cancellationToken).ConfigureAwait(false);
            await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            return AppResponses.Success("Employee updated.", employee.ToEmployeeResponse());
        }
        catch (Exception ex)
        {
            LogDefinitions.LogEmployeeRegistrationFailed(logger, command.Request.Email, ex);
            throw;
        }
    }
}
