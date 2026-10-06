using ST.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;
using QRCoder;

namespace ST.UI.Blazor.Features.IAM.Users.Contracts.Implementations;

internal sealed class AuthenticatorQrCodeService : IAuthenticatorQrCodeService
{
    public string GenerateDataUri(string authenticatorUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticatorUri);

        using var generator = new QRCodeGenerator();
        using var qrCodeData = generator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var bytes = qrCode.GetGraphic(12);

        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
