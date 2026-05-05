using Libr4.Shared.Kernel.Errors;

namespace Libr4.Auth.Domain.Users;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyExists = Error.Conflict("auth.email_exists", "Email is already registered");
    public static readonly Error InvalidCredentials = Error.Unauthorized("auth.invalid_credentials", "Invalid email or password");
    public static readonly Error AccountLocked = Error.Forbidden("auth.account_locked", "Account temporarily locked due to too many failed attempts");
    public static readonly Error AccountInactive = Error.Forbidden("auth.account_inactive", "Account is inactive");
    public static readonly Error UserNotFound = Error.NotFound("auth.user_not_found", "User not found");
    public static readonly Error InvalidRefreshToken = Error.Unauthorized("auth.invalid_refresh", "Invalid or expired refresh token");
    public static readonly Error TwoFactorRequired = Error.Unauthorized("auth.2fa_required", "Two-factor code required");
    public static readonly Error TwoFactorInvalid = Error.Unauthorized("auth.2fa_invalid", "Invalid two-factor code");
    public static readonly Error TwoFactorAlreadyEnabled = Error.Conflict("auth.2fa_enabled", "Two-factor authentication already enabled");
    public static readonly Error WeakPassword = Error.Validation("auth.weak_password", "Password must be at least 8 characters with letters and digits");
    public static readonly Error InvalidToken = Error.Validation("auth.invalid_token", "Token is invalid or expired");
    public static readonly Error EmailAlreadyConfirmed = Error.Conflict("auth.email_already_confirmed", "Email is already confirmed");
}
