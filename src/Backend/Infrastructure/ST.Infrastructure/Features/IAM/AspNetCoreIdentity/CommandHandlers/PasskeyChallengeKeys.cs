using System.Security.Cryptography;
using System.Text;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal static class PasskeyChallengeKeys
{
    internal static string Registration(string userId) => $"PasskeyRegistration:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId)))}";
    internal static string Assertion(Guid correlationId) => $"PasskeyAssertion:{correlationId:D}";
}
