using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ST.Infrastructure.Configuration;
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string? Secret { get; set; }
    public string? SecretKey { get; set; } // Keep this for backward compatibility
    public string? Issuer { get; set; }
    public string[]? ValidIssuers { get; set; }
    public string? Audience { get; set; }
    public string[]? ValidAudiences { get; set; }
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryHours { get; set; } = 8;
    public int ClockSkew { get; set; } = 1;

    // Helper method to get the actual security key
    public string GetSecurityKey()
    {
        if (!string.IsNullOrWhiteSpace(SecretKey))
            return SecretKey;

        if (!string.IsNullOrWhiteSpace(Secret))
            return Secret;

        throw new InvalidOperationException("JWT security key not configured. Set either 'Secret' or 'SecretKey' in configuration.");
    }

    // Helper method to get SymmetricSecurityKey
    public SymmetricSecurityKey GetSymmetricSecurityKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecurityKey()));
    }
}
