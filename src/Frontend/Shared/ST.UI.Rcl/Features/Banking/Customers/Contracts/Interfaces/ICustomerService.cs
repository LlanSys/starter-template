using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Banking.Customers.Dtos;

namespace ST.UI.Rcl.Features.Banking.Customers.Contracts.Interfaces;

public interface ICustomerService
{
    Task<AppResponse<PagedResponse<CustomerResponse, Guid>>> SearchAsync(CustomerSearchRequest request);
    Task<AppResponse<CustomerResponse>> GetByIdAsync(Guid id);
    Task<AppResponse<CustomerResponse>> CreateAsync(CreateCustomerRequest request);
    Task<AppResponse<CustomerResponse>> UpdateAsync(Guid id, UpdateCustomerRequest request);
    Task<AppResponse<bool>> DeleteAsync(Guid id);
}
