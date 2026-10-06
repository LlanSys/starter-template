using System.Collections.ObjectModel;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.HR.Employees.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Employees.Dtos;
using ST.SharedKernel.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Employees.QueryHandlers;



internal sealed class SearchEmployeesQueryHandler(IHrUnitOfWork unitOfWork, ILogger<SearchEmployeesQueryHandler> logger)
    : IRequestHandler<SearchEmployeesQuery, AppResponse<PagedResponse<EmployeeResponse, Guid>>>
{
    public async Task<AppResponse<PagedResponse<EmployeeResponse, Guid>>> Handle(SearchEmployeesQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var request = query.SearchRequest;
            var pageSize = Math.Clamp(request.PageSize, 1, 50);

            IQueryable<Employee> ApplyFilters(IQueryable<Employee> employees)
            {
                if (!string.IsNullOrWhiteSpace(request.GlobalSearch))
                {
                    var search = request.GlobalSearch.Trim();
                    employees = employees.Where(employee =>
                        employee.Number.Contains(search)
                        || employee.FirstName.Contains(search)
                        || employee.LastName.Contains(search)
                        || employee.Email.Contains(search)
                        || employee.PhoneNumber.Contains(search)
                        || employee.IdNumber.Contains(search));
                }

                if (request.DepartmentId.HasValue)
                {
                    employees = employees.Where(employee => employee.DepartmentId == request.DepartmentId.Value);
                }

                if (request.ManagerId.HasValue)
                {
                    employees = employees.Where(employee => employee.ManagerId == request.ManagerId.Value);
                }

                return employees;
            }

            var totalCount = await unitOfWork.EmployeeRepository
                .CountAsync(ApplyFilters, cancellationToken)
                .ConfigureAwait(false);

            IQueryable<Employee> ApplyPaging(IQueryable<Employee> employees)
            {
                employees = ApplyFilters(employees);

                if (request.Cursor.HasValue && request.Cursor != Guid.Empty)
                {
                    employees = employees.Where(employee => employee.Id.CompareTo(request.Cursor.Value) > 0);
                }

                return employees
                    .OrderBy(static employee => employee.Id)
                    .Take(pageSize + 1);
            }

            var page = await unitOfWork.EmployeeRepository.ListAsync(ApplyPaging, cancellationToken).ConfigureAwait(false);

            var hasNextPage = page.Count > pageSize;
            if (hasNextPage)
            {
                page.RemoveAt(page.Count - 1);
            }

            var departmentIds = page.Select(static employee => employee.DepartmentId).Distinct().ToArray();
            var managerIds = page
                .Select(static employee => employee.ManagerId)
                .Where(static managerId => managerId.HasValue)
                .Select(static managerId => managerId!.Value)
                .Distinct()
                .ToArray();
            var departmentRows = await unitOfWork.DepartmentRepository
                .ListAsync(
                    departments => departments
                        .Where(department => departmentIds.Contains(department.Id))
                        .Select(static department => new { department.Id, department.Name }),
                    cancellationToken)
                .ConfigureAwait(false);
            var departments = departmentRows.ToDictionary(static department => department.Id, static department => department.Name);
            var managers = managerIds.Length == 0
                ? new Dictionary<Guid, string>()
                : (await unitOfWork.EmployeeRepository
                    .ListAsync(
                        employees => employees
                            .Where(manager => managerIds.Contains(manager.Id))
                            .Select(static manager => new { manager.Id, Name = $"{manager.FirstName} {manager.LastName}" }),
                        cancellationToken)
                    .ConfigureAwait(false))
                    .ToDictionary(static manager => manager.Id, static manager => manager.Name);

            var items = page
                .Select(employee => employee.ToEmployeeResponse(
                    departments.GetValueOrDefault(employee.DepartmentId, string.Empty),
                    employee.ManagerId.HasValue ? managers.GetValueOrDefault(employee.ManagerId.Value, string.Empty) : string.Empty))
                .OrderBy(static employee => employee.Number, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var nextCursor = hasNextPage ? items[^1].Id : (Guid?)null;
            var result = new PagedResponse<EmployeeResponse, Guid>(
                new Collection<EmployeeResponse>(items),
                totalCount,
                1,
                pageSize,
                request.Cursor is null || request.Cursor == Guid.Empty,
                nextCursor ?? Guid.Empty);

            return AppResponses.Success(result);
        }
        catch (Exception ex)
        {
            LogDefinitions.LogStaffMembersFetchFailed(logger, ex);
            throw;
        }
    }
}
