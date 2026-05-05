namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.FeatureFlags;

/// <summary>
/// Service for managing and evaluating feature flags.
/// Supports multi-level gating: global, per-user, and per-run.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Checks if a feature flag is enabled for the given context.
    /// </summary>
    /// <param name="flagName">The name of the feature flag.</param>
    /// <param name="userId">Optional user ID for per-user gating.</param>
    /// <param name="runId">Optional run ID for per-run gating.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    Task<bool> IsEnabledAsync(string flagName, string? userId = null, Guid? runId = null);

    /// <summary>
    /// Gets the value of a feature flag if it's a string type.
    /// </summary>
    /// <param name="flagName">The name of the feature flag.</param>
    /// <param name="userId">Optional user ID for per-user gating.</param>
    /// <param name="runId">Optional run ID for per-run gating.</param>
    /// <returns>The flag value or null if not found.</returns>
    Task<string?> GetValueAsync(string flagName, string? userId = null, Guid? runId = null);

    /// <summary>
    /// Sets a feature flag value for a specific scope.
    /// </summary>
    /// <param name="flagName">The name of the feature flag.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="scope">The scope (global, user, or run).</param>
    /// <param name="scopeId">The ID for the scope (user ID or run ID).</param>
    Task SetFlagAsync(string flagName, string value, FeatureFlagScope scope, string? scopeId = null);

    /// <summary>
    /// Gets all feature flags for a given scope.
    /// </summary>
    /// <param name="scope">The scope to query.</param>
    /// <param name="scopeId">The ID for the scope.</param>
    /// <returns>Dictionary of flag names to values.</returns>
    Task<Dictionary<string, string>> GetFlagsAsync(FeatureFlagScope scope, string? scopeId = null);
}

/// <summary>
/// Scope for feature flag evaluation.
/// </summary>
public enum FeatureFlagScope
{
    /// <summary>
    /// Global flag that applies to all users and runs.
    /// </summary>
    Global,

    /// <summary>
    /// User-specific flag that applies only to a specific user.
    /// </summary>
    User,

    /// <summary>
    /// Run-specific flag that applies only to a specific generation run.
    /// </summary>
    Run
}
