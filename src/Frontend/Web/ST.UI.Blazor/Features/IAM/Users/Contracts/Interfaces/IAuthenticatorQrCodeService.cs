namespace ST.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;

internal interface IAuthenticatorQrCodeService
{
    string GenerateDataUri(string authenticatorUri);
}
