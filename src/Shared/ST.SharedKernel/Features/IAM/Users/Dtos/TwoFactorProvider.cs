namespace ST.SharedKernel.Features.IAM.Users.Dtos;
public record TwoFactorProvider(
    string Value,
    string Text,
    string DisplayName,
    string Icon,
    bool IsEnabled,
    bool Selected,
    bool IsDefault,
    string? MaskedDestination = null
    );
