using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Users;
using Microsoft.Extensions.Logging;

namespace Libr4.Auth.Application;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository userRepository, IJwtTokenService jwtService, IPasswordHasher passwordHasher, ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        user.UpdateLastLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        user.AddRefreshToken(RefreshToken.Create(user.Id, refreshToken, DateTimeOffset.UtcNow.AddDays(7)));
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("User {UserId} logged in", user.Id);
        return new AuthResponse(accessToken, refreshToken, DateTimeOffset.UtcNow.AddHours(1));
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.GetByEmailAsync(request.Email, cancellationToken) != null)
        {
            throw new InvalidOperationException("User already exists");
        }

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.Email, request.Username, passwordHash);
        await _userRepository.AddAsync(user, cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        user.AddRefreshToken(RefreshToken.Create(user.Id, refreshToken, DateTimeOffset.UtcNow.AddDays(7)));
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("User {UserId} registered", user.Id);
        return new AuthResponse(accessToken, refreshToken, DateTimeOffset.UtcNow.AddHours(1));
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken, cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var token = user.RefreshTokens.FirstOrDefault(t => t.Token == refreshToken && t.IsActive());
        if (token == null)
        {
            throw new UnauthorizedAccessException("Refresh token expired or revoked");
        }

        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        user.RevokeRefreshToken(token.Id);
        user.AddRefreshToken(RefreshToken.Create(user.Id, newRefreshToken, DateTimeOffset.UtcNow.AddDays(7)));
        await _userRepository.UpdateAsync(user, cancellationToken);

        return new AuthResponse(newAccessToken, newRefreshToken, DateTimeOffset.UtcNow.AddHours(1));
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken, cancellationToken);
        if (user != null)
        {
            user.RevokeRefreshToken(user.RefreshTokens.First(t => t.Token == refreshToken).Id);
            await _userRepository.UpdateAsync(user, cancellationToken);
        }
    }
}