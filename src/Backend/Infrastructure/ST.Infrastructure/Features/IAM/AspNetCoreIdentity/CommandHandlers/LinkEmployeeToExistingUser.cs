using ST.SharedKernel.Extensions;
using ST.Application.Features.Banking.Customers.Mappings;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Application.Features.Shared.EmailTemplates.Mappings;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.HR.Employees.Contracts.Interfaces;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.HR.Employees.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Users.Enums;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Employees.Dtos;
using ST.SharedKernel.Features.Shared.Phone;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class LinkEmployeeToExistingUser(
    IHrUnitOfWork hrUnitOfWork,
    UserManager<AppUser> userManager,
    IEmployeeNumberGenerator employeeNumberGenerator)
    : IRequestHandler<LinkEmployeeToExistingUserCommand, AppResponse<EmployeeResponse>>
{
    public async Task<AppResponse<EmployeeResponse>> Handle(LinkEmployeeToExistingUserCommand command, CancellationToken ct)
    {
        var existingUser = await userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.NationalId == command.NationalId, ct)
            .ConfigureAwait(false);

        if (existingUser is null)
        {
            return AppResponses.Failure<EmployeeResponse>("No existing user found with this National ID. Use CreateEmployee instead.");
        }

        if (existingUser.EmployeeId.HasValue)
        {
            return AppResponses.Failure<EmployeeResponse>("This user already has an employee record linked.");
        }

        var employeeNumber = await employeeNumberGenerator.GenerateAsync(command.EmployeeDetails.DepartmentId, ct).ConfigureAwait(false);
        var phone = PhoneNumberFormatter.Normalize(
            command.EmployeeDetails.CountryCode,
            command.EmployeeDetails.PhoneNationalNumber,
            command.EmployeeDetails.PhoneNumber);

        var employee = Employee.Create(
            employeeNumber,
            command.EmployeeDetails.Email,
            existingUser.FirstName,
            existingUser.LastName,
            command.NationalId,
            phone.CountryCode,
            phone.NationalNumber,
            phone.E164,
            command.EmployeeDetails.DepartmentId,
            command.EmployeeDetails.ManagerId,
            command.CreatedBy);

        await hrUnitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await hrUnitOfWork.EmployeeRepository.CreateAsync(employee, ct).ConfigureAwait(false);

            existingUser.LinkToEmployee(employee.Id);

            await userManager.UpdateAsync(existingUser).ConfigureAwait(false);
            await userManager.AddToRoleAsync(existingUser, Roles.Employee.ToDisplayString()).ConfigureAwait(false);

            await hrUnitOfWork.CompleteAsync(ct).ConfigureAwait(false);
            return true;

        }, ct).ConfigureAwait(false);

        return AppResponses.Success("Employee record created and linked to existing user.",
            employee.ToEmployeeResponse());
    }
}
