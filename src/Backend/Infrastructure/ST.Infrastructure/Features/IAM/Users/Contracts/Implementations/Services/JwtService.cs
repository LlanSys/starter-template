using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;

internal sealed class JwtService(
    IOptions<JwtSettings> jwtSettings,
    ILogger<JwtService> logger) : IJwtService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly ILogger<JwtService> _logger = logger;

    public async Task<string> CreateTokenAsync(List<Claim> userClaims)
    {
        var now = DateTime.UtcNow;
        var authSigningKey = _jwtSettings.GetSymmetricSecurityKey();
        var credentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: userClaims,
            notBefore: now,
            expires: now.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> CreateTempTokenAsync(List<Claim> userClaims, TimeSpan expiry)
    {
        var now = DateTime.UtcNow;
        var authSigningKey = _jwtSettings.GetSymmetricSecurityKey();
        var credentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: userClaims,
            notBefore: now,
            expires: now.Add(expiry),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateRefreshToken()
    {
        try
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);
        }
        catch (Exception)
        {
            ServiceLogDefinitions.LogFailedToGenerateRefreshToken(_logger, string.Empty);
            throw;
        }
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ServiceLogDefinitions.LogInvalidToken(_logger);
                return new();
            }

            var securityKey = _jwtSettings.GetSecurityKey();
            if (string.IsNullOrWhiteSpace(securityKey))
            {
                ServiceLogDefinitions.LogJwtSecurityKeyNotConfigured(_logger);
                return new();

            }

            var tokenHandler = new JwtSecurityTokenHandler();

            // Create token validation parameters with NO expiry validation
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.FromMinutes(Convert.ToDouble(_jwtSettings.ClockSkew))
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                ServiceLogDefinitions.LogInvalidJwtAlgorithm(_logger);
                return new();
            }

            return principal;
        }
        catch (SecurityTokenException ex)
        {
            ServiceLogDefinitions.LogSecurityTokenException(_logger, ex);
            throw;
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogErrorParsingExpiredToken(_logger, ex);
            throw;
        }
    }

    public DateTimeOffset GetTokenExpiry(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            return DateTimeOffset.FromUnixTimeSeconds(jsonToken.Payload.Expiration ?? 0);

        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogErrorParsingExpiredToken(_logger, ex);
            throw;
        }
    }

    public bool IsTokenValid(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token is null");

            var tokenHandler = new JwtSecurityTokenHandler();
            var clockSkew = Convert.ToDouble(_jwtSettings.ClockSkew);

            TokenValidationParameters tokenValidationParameters = new()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _jwtSettings.GetSymmetricSecurityKey(),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(clockSkew)

            };

            tokenHandler.ValidateToken(token, tokenValidationParameters, out _);

            return true;
        }
        catch (SecurityTokenExpiredException ex)
        {
            ServiceLogDefinitions.LogTokenExpired(_logger, ex);
            throw;
        }
        catch (SecurityTokenValidationException ex)
        {
            ServiceLogDefinitions.LogInvalidTokenWithException(_logger, ex);
            throw;
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogUnexpectedTokenValidationError(_logger, ex);
            throw;
        }
    }


}
