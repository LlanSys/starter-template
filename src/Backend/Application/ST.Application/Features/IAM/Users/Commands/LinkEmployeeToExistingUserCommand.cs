using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Employees.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.IAM.Users.Commands;

public sealed record LinkEmployeeToExistingUserCommand(string NationalId, CreateEmployeeRequest EmployeeDetails, string CreatedBy)
 : IRequest<AppResponse<EmployeeResponse>>;
