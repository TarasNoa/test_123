namespace Libr4.Auth.Application.Dtos;

public sealed record RegisterRequest(
    string Email,
    string DisplayName,
    string Password,
    string Role,
    string? Phone = null,
    string? Country = null,
    string? City = null,
    string? CompanyName = null,
    string? Industry = null,
    string? CompanySize = null,
    string? Website = null,
    string? Skills = null,
    string? Experience = null,
    string? HourlyRate = null,
    string? Specialization = null,
    string? LinkedInUrl = null);

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
    DateTimeOffset CreatedAt,
    string? Role = null,
    string? Phone = null,
    string? Country = null,
    string? City = null,
    string? CompanyName = null,
    string? Industry = null,
    string? CompanySize = null,
    string? Website = null,
    IReadOnlyCollection<string>? Skills = null,
    string? Experience = null,
    decimal? HourlyRate = null,
    string? Specialization = null,
    string? LinkedInUrl = null,
    string? CvUrl = null,
    string? AvatarUrl = null,
    string? CoverUrl = null,
    string? Bio = null,
    string? Location = null,
    decimal? Rating = null,
    decimal? TotalEarnings = null,
    decimal? TotalSpent = null,
    int? CompletedTasks = null,
    bool? IsFreelancer = null,
    bool? IsClient = null);

public sealed record TwoFactorSetupResponse(string OtpAuthUri, string QrPngBase64);

public sealed record TwoFactorVerifyRequest(string Code);

public sealed record TwoFactorDisableRequest(string Password);

public sealed record ConfirmEmailRequest(string Token);

public sealed record PasswordResetRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
