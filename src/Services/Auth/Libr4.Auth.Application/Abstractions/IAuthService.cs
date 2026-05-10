using System;
using System.Threading.Tasks;

namespace Libr4.Auth.Application.Abstractions;

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Username, string Password);
public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}