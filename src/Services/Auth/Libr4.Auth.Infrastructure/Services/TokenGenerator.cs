using System.Security.Cryptography;
using System.Text;
using Libr4.Auth.Application.Abstractions;

namespace Libr4.Auth.Infrastructure.Services;

public sealed class TokenGenerator : ITokenGenerator
{
    public (string plain, string hash) Create()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var plain = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (plain, Hash(plain));
    }

    public string Hash(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(bytes);
    }
}
