using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.FeatureFlags;

#if INTERNAL
/// <summary>
/// In-memory implementation of feature flag service.
/// Supports multi-level gating: global, per-user, and per-run.
/// INTERNAL: This service is for internal use only and will not be included in public builds.
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly ILogger<FeatureFlagService> _logger;
    private readonly Dictionary<string, string> _globalFlags = new();
    private readonly Dictionary<string, Dictionary<string, string>> _userFlags = new();
    private readonly Dictionary<string, Dictionary<string, string>> _runFlags = new();

    public FeatureFlagService(ILogger<FeatureFlagService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(string flagName, string? userId = null, Guid? runId = null)
    {
        var value = await GetValueAsync(flagName, userId, runId);
        
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (bool.TryParse(value, out var boolValue))
        {
            return boolValue;
        }

        // Treat any non-empty value as enabled
        return true;
    }

    public async Task<string?> GetValueAsync(string flagName, string? userId = null, Guid? runId = null)
    {
        // Priority: run-specific > user-specific > global
        
        // Check run-specific flag
        if (runId.HasValue)
        {
            var runIdStr = runId.Value.ToString();
            if (_runFlags.TryGetValue(runIdStr, out var runFlags) && runFlags.TryGetValue(flagName, out var runValue))
            {
                _logger.LogDebug("Feature flag {FlagName} found in run scope: {Value}", flagName, runValue);
                return runValue;
            }
        }

        // Check user-specific flag
        if (!string.IsNullOrEmpty(userId))
        {
            if (_userFlags.TryGetValue(userId, out var userFlags) && userFlags.TryGetValue(flagName, out var userValue))
            {
                _logger.LogDebug("Feature flag {FlagName} found in user scope: {Value}", flagName, userValue);
                return userValue;
            }
        }

        // Check global flag
        if (_globalFlags.TryGetValue(flagName, out var globalValue))
        {
            _logger.LogDebug("Feature flag {FlagName} found in global scope: {Value}", flagName, globalValue);
            return globalValue;
        }

        _logger.LogDebug("Feature flag {FlagName} not found in any scope", flagName);
        return null;
    }

    public async Task SetFlagAsync(string flagName, string value, FeatureFlagScope scope, string? scopeId = null)
    {
        switch (scope)
        {
            case FeatureFlagScope.Global:
                _globalFlags[flagName] = value;
                _logger.LogInformation("Set global feature flag: {FlagName} = {Value}", flagName, value);
                break;

            case FeatureFlagScope.User:
                if (string.IsNullOrEmpty(scopeId))
                {
                    throw new ArgumentException("User ID is required for user scope", nameof(scopeId));
                }
                if (!_userFlags.ContainsKey(scopeId))
                {
                    _userFlags[scopeId] = new Dictionary<string, string>();
                }
                _userFlags[scopeId][flagName] = value;
                _logger.LogInformation("Set user feature flag: {FlagName} = {Value} for user {UserId}", flagName, value, scopeId);
                break;

            case FeatureFlagScope.Run:
                if (string.IsNullOrEmpty(scopeId))
                {
                    throw new ArgumentException("Run ID is required for run scope", nameof(scopeId));
                }
                if (!_runFlags.ContainsKey(scopeId))
                {
                    _runFlags[scopeId] = new Dictionary<string, string>();
                }
                _runFlags[scopeId][flagName] = value;
                _logger.LogInformation("Set run feature flag: {FlagName} = {Value} for run {RunId}", flagName, value, scopeId);
                break;

            default:
                throw new ArgumentException($"Unknown scope: {scope}", nameof(scope));
        }

        await Task.CompletedTask;
    }

    public async Task<Dictionary<string, string>> GetFlagsAsync(FeatureFlagScope scope, string? scopeId = null)
    {
        Dictionary<string, string> flags = scope switch
        {
            FeatureFlagScope.Global => _globalFlags,
            FeatureFlagScope.User => scopeId != null && _userFlags.TryGetValue(scopeId, out var userFlags) ? userFlags : new Dictionary<string, string>(),
            FeatureFlagScope.Run => scopeId != null && _runFlags.TryGetValue(scopeId, out var runFlags) ? runFlags : new Dictionary<string, string>(),
            _ => throw new ArgumentException($"Unknown scope: {scope}", nameof(scope))
        };

        return await Task.FromResult(new Dictionary<string, string>(flags));
    }
}
#endif
