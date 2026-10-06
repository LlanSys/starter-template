using ST.Application.Features.Banking.Customers.Mappings;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Application.Features.Shared.EmailTemplates.Mappings;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.HR.Employees.Entities;
using ST.Application.Features.HR.Employees.Contracts.Interfaces;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Employees.Dtos;
using ST.SharedKernel.Features.Shared.Phone;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Employees.CommandHandlers;



internal sealed class CreateEmployeeCommandHandler(
    IHrUnitOfWork unitOfWork,
    IEmployeeNumberGenerator numberGenerator,
    ILogger<CreateEmployeeCommandHandler> logger)
    : IRequestHandler<CreateEmployeeCommand, AppResponse<EmployeeResponse>>
{
    public async Task<AppResponse<EmployeeResponse>> Handle(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        try
        {
            var duplicateEmail = await unitOfWork.EmployeeRepository
                .AnyAsync(e => e.Email == request.Email, cancellationToken)
                .ConfigureAwait(false);

            if (duplicateEmail)
            {
                LogDefinitions.LogEmployeeDuplicateRegistration(logger, request.Email);
                return AppResponses.Failure<EmployeeResponse>("An employee with this email already exists.");
            }

            var result = await unitOfWork.ExecuteInTransactionWithRetryAsync(async () =>
            {
                var employeeNumber = await numberGenerator.GenerateAsync(request.DepartmentId, cancellationToken).ConfigureAwait(false);
                var phone = PhoneNumberFormatter.Normalize(
                    request.CountryCode,
                    request.PhoneNationalNumber,
                    request.PhoneNumber);

                var entityToCreate = Employee.Create(
                        employeeNumber,
                        request.Email,
                        request.FirstName,
                        request.LastName,
                        request.IdNumber,
                        phone.CountryCode,
                        phone.NationalNumber,
                        phone.E164,
                        request.DepartmentId,
                        request.ManagerId,
                        command.User);

                var createdEmployee = await unitOfWork.EmployeeRepository.CreateAsync(entityToCreate).ConfigureAwait(false);

                return AppResponses.Success(
                    "Account created successfully! Please check your email to confirm your account.",
                    createdEmployee.ToEmployeeResponse());

            }).ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            LogDefinitions.LogEmployeeRegistrationFailed(logger, request.Email, ex);

            throw;
        }
    }
}
