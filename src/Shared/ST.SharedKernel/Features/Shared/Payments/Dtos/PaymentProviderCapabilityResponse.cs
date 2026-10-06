namespace ST.SharedKernel.Features.Shared.Payments.Dtos;

public sealed record PaymentProviderCapabilityResponse(
    string Provider,
    string DisplayName,
    bool IsEnabled,
    bool IsConfigured,
    string Environment,
    bool SupportsRedirectCheckout,
    bool RequiresPayerPhoneNumber,
    bool SupportsC2BAdministration);
