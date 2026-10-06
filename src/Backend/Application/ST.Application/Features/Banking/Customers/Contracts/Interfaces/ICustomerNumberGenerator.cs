namespace ST.Application.Features.Banking.Customers.Contracts.Interfaces;

internal interface ICustomerNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}
