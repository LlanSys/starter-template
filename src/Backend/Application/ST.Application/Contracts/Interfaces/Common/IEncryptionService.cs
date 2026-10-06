using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Contracts.Interfaces.Common;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string HashCode(string code);
    bool VerifyCode(string code, string hash);
}
