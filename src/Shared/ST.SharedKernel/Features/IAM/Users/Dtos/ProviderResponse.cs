namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public record ProviderResponse(
    List<TwoFactorProvider>? Providers,
    string PreferredProvider);
