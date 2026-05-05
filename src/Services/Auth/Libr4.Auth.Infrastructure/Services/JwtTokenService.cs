using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Users;
using Libr4.Shared.Kernel.Time;
using Libr4.Shared.Web.Auth;
using Microsoft.IdentityModel.Tokens;

namespace Libr4.Auth.Infrastructure.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(JwtOptions options, IClock clock)
    {
        _options = options;
        _clock = clock;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public AccessTokenResult CreateAccessToken(User user)
    {
        var expires = _clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("display_name", user.DisplayName),
        };
        foreach (var r in user.Roles)
            claims.Add(new Claim("role", r.Role.ToString()));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: _clock.UtcNow.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: _credentials);

        var str = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessTokenResult(str, expires);
    }

    public (string plain, string hash) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        var plain = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (plain, HashRefreshToken(plain));
    }

    public string HashRefreshToken(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(bytes);
    }
}
