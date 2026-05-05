namespace Libr4.Auth.Application.Dtos;

public sealed record RegisterRequest(string Email, string DisplayName, string Password);

public sealed record LoginRequest(string Email, string Password, string? TwoFactorCode);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    DateTimeOffset CreatedAt);

public sealed record TwoFactorSetupResponse(string OtpAuthUri, string QrPngBase64);

public sealed record TwoFactorVerifyRequest(string Code);

public sealed record TwoFactorDisableRequest(string Password);

public sealed record ConfirmEmailRequest(string Token);

public sealed record PasswordResetRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
