using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace ST.Infrastructure.Security;

internal static class SingleUseCredential
{
    public static string Create() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string credential) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(credential)));

    public static bool Matches(string? expected, string credential) =>
        expected is not null && credential.Length == 43
        && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(Hash(credential)));
}
