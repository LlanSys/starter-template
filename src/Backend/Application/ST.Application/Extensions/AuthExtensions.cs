using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ST.Application.Extensions;

public static class AuthExtensions
{
    public static string SecurityKey(out string hashString)
    {
        string secureRandomString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var bytes = Encoding.UTF8.GetBytes(secureRandomString);
        var hash = SHA512.HashData(bytes);
        hashString = Convert.ToBase64String(hash);

        return hashString;

    }
}
