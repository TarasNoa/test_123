using Libr4.IDE.Application.AutonomousAppGeneration.Runtime.Docker;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

/// <summary>
/// Selects the active isolated runtime provider and applies an explicit fallback chain.
/// This keeps orchestration stable when a preferred provider is temporarily unavailable
/// (for example Docker daemon not running on developer laptops).
/// </summary>
public sealed class RuntimeProviderRouter : IIsolatedRuntime
{
    private readonly IReadOnlyDictionary<string, IIsolatedRuntime> _providers;
    private readonly IReadOnlyList<string> _selectionOrder;
    private readonly ILogger<RuntimeProviderRouter> _logger;
    private readonly IRuntimeDiagnostics _diagnostics;

    public string ProviderName { get; }

    public RuntimeProviderRouter(
        string preferredProvider,
        bool allowFallbackToProcess,
        DockerIsolatedRuntime docker,
        WslIsolatedRuntime wsl,
        HyperVRuntime hyperV,
        ProcessIsolatedRuntime process,
        IRuntimeDiagnostics diagnostics,
        ILogger<RuntimeProviderRouter> logger)
    {
        _logger = logger;
        _diagnostics = diagnostics;
        ProviderName = Normalize(preferredProvider);

        _providers = new Dictionary<string, IIsolatedRuntime>(StringComparer.OrdinalIgnoreCase)
        {
            ["docker"] = docker,
            ["wsl"] = wsl,
            ["hyperv"] = hyperV,
            ["process"] = process
        };

        var order = new List<string> { ProviderName };
        if (!string.Equals(ProviderName, "docker", StringComparison.OrdinalIgnoreCase)) order.Add("docker");
        if (!string.Equals(ProviderName, "wsl", StringComparison.OrdinalIgnoreCase)) order.Add("wsl");
        if (!string.Equals(ProviderName, "hyperv", StringComparison.OrdinalIgnoreCase)) order.Add("hyperv");
        if (allowFallbackToProcess && !string.Equals(ProviderName, "process", StringComparison.OrdinalIgnoreCase)) order.Add("process");
        _selectionOrder = order.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<IRuntimeSession> StartSessionAsync(
        string image,
        string hostMountPath,
        CancellationToken ct = default)
    {
        var failures = new List<string>();

        foreach (var providerKey in _selectionOrder)
        {
            if (!_providers.TryGetValue(providerKey, out var provider))
            {
                failures.Add($"{providerKey}: provider is not registered");
                continue;
            }

            try
            {
                var session = await provider.StartSessionAsync(image, hostMountPath, ct);
                _diagnostics.RecordAttempt(
                    preferredProvider: ProviderName,
                    attemptedProvider: provider.ProviderName,
                    succeeded: true,
                    usedAsFallback: !string.Equals(provider.ProviderName, ProviderName, StringComparison.OrdinalIgnoreCase),
                    sessionId: session.SessionId,
                    errorMessage: null);

                if (!string.Equals(provider.ProviderName, ProviderName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Isolated runtime fallback activated. Preferred={Preferred}, Actual={Actual}",
                        ProviderName, provider.ProviderName);
                }

                return session;
            }
            catch (Exception ex)
            {
                failures.Add($"{provider.ProviderName}: {ex.Message}");
                _diagnostics.RecordAttempt(
                    preferredProvider: ProviderName,
                    attemptedProvider: provider.ProviderName,
                    succeeded: false,
                    usedAsFallback: !string.Equals(provider.ProviderName, ProviderName, StringComparison.OrdinalIgnoreCase),
                    sessionId: null,
                    errorMessage: ex.Message);
                _logger.LogWarning(
                    ex,
                    "Runtime provider {Provider} failed to start session. Trying next provider.",
                    provider.ProviderName);
            }
        }

        throw new InvalidOperationException(
            $"No isolated runtime provider could start a session. Attempts: {string.Join(" | ", failures)}");
    }

    private static string Normalize(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider)) return "docker";
        return provider.Trim().ToLowerInvariant();
    }
}