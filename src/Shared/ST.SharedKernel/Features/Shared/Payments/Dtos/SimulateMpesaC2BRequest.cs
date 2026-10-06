namespace ST.SharedKernel.Features.Shared.Payments.Dtos;

public sealed record SimulateMpesaC2BRequest(
    decimal Amount,
    string PhoneNumber,
    string BillRefNumber);
