namespace Libr4.Shared.Contracts.Authorization;

/// <summary>
/// User roles for access control.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Public users with limited access to basic features only.
    /// </summary>
    Public,

    /// <summary>
    /// Developers with access to development features and internal tools.
    /// </summary>
    Developer,

    /// <summary>
    /// Operators with full access to all features including administrative functions.
    /// </summary>
    Operator
}

/// <summary>
/// Service for determining the current user's role.
/// </summary>
public interface IUserRoleProvider
{
    /// <summary>
    /// Gets the role for the current user.
    /// </summary>
    /// <param name="userId">Optional user ID. If null, uses the current authenticated user.</param>
    /// <returns>The user's role.</returns>
    Task<UserRole> GetRoleAsync(string? userId = null);

    /// <summary>
    /// Checks if the current user has the required role or higher.
    /// </summary>
    /// <param name="requiredRole">The minimum required role.</param>
    /// <param name="userId">Optional user ID. If null, uses the current authenticated user.</param>
    /// <returns>True if the user has the required role or higher.</returns>
    Task<bool> HasRoleOrHigherAsync(UserRole requiredRole, string? userId = null);
}
