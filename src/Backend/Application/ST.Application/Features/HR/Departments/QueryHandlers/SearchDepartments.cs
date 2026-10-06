using System.Collections.ObjectModel;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.HR.Departments.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.HR.Departments.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Departments.Dtos;
using ST.SharedKernel.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.HR.Departments.QueryHandlers;



internal sealed class SearchDepartmentsQueryHandler(IHrUnitOfWork unitOfWork, ILogger<SearchDepartmentsQueryHandler> logger)
    : IRequestHandler<SearchDepartmentsQuery, AppResponse<PagedResponse<DepartmentResponse, Guid>>>
{
    public async Task<AppResponse<PagedResponse<DepartmentResponse, Guid>>> Handle(SearchDepartmentsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var request = query.SearchRequest;
            var pageSize = Math.Clamp(request.PageSize, 1, 50);

            IQueryable<Department> ApplyFilters(IQueryable<Department> departments)
            {
                if (!string.IsNullOrWhiteSpace(request.GlobalSearch))
                {
                    var search = request.GlobalSearch.Trim();
                    departments = departments.Where(department =>
                        department.Code.Contains(search)
                        || department.Name.Contains(search)
                        || department.Description.Contains(search));
                }

                if (request.IsActive.HasValue)
                {
                    departments = departments.Where(department => department.IsActive == request.IsActive.Value);
                }

                return departments;
            }

            var totalCount = await unitOfWork.DepartmentRepository.CountAsync(ApplyFilters, cancellationToken).ConfigureAwait(false);

            IQueryable<Department> ApplyPaging(IQueryable<Department> departments)
            {
                departments = ApplyFilters(departments);

                if (request.Cursor.HasValue && request.Cursor != Guid.Empty)
                {
                    departments = departments.Where(department => department.Id.CompareTo(request.Cursor.Value) > 0);
                }

                return departments
                    .OrderBy(static department => department.Id)
                    .Take(pageSize + 1);
            }

            var page = await unitOfWork.DepartmentRepository.ListAsync(ApplyPaging, cancellationToken).ConfigureAwait(false);

            var hasNextPage = page.Count > pageSize;
            if (hasNextPage)
            {
                page.RemoveAt(page.Count - 1);
            }

            var items = page
                .Select(static department => department.ToDepartmentResponse())
                .OrderBy(static department => department.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var nextCursor = hasNextPage ? page[^1].Id : (Guid?)null;
            var result = new PagedResponse<DepartmentResponse, Guid>(
                new Collection<DepartmentResponse>(items),
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
