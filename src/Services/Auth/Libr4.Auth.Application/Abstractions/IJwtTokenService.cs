using Libr4.Auth.Domain.Users;

namespace Libr4.Auth.Application.Abstractions;

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(User user);
    (string plain, string hash) CreateRefreshToken();
    string HashRefreshToken(string plain);
}

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);
