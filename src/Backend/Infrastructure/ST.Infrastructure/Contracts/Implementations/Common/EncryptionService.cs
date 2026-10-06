using ST.Application.Contracts.Interfaces.Common;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ST.Infrastructure.Contracts.Implementations.Common;

internal sealed class EncryptionService(IDataProtectionProvider dataProtectionProvider) : IEncryptionService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("TotpSecrets");

    public string Encrypt(string plainText)
    {
        return _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        return _protector.Unprotect(cipherText);
    }

    public string HashCode(string code)
    {
        var hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(hashedBytes);
    }

    public bool VerifyCode(string code, string hash)
    {
        var codeHash = HashCode(code);
        return codeHash.Equals(hash, StringComparison.Ordinal);
    }
}
