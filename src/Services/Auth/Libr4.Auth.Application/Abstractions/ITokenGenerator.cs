namespace Libr4.Auth.Application.Abstractions;

public interface ITokenGenerator
{
    /// <summary>Creates a cryptographically strong URL-safe random token plus its SHA-256 hash.</summary>
    (string plain, string hash) Create();
    string Hash(string plain);
}
