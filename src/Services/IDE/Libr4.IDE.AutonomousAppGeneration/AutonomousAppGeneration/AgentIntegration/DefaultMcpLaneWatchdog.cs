using System.Collections.Concurrent;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class DefaultMcpLaneWatchdog : IMcpLaneWatchdog
{
    private readonly IMcpServerPreflight _preflight;
    private readonly IMcpToolRegistry _registry;
    private readonly IOptions<McpExecutionOptions> _options;
    private readonly ILogger<DefaultMcpLaneWatchdog> _logger;
    private readonly ConcurrentDictionary<string, McpLaneWatchdogSnapshot> _snapshots = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<McpLaneWatchdogHistoryEntry>> _history = new();

    public DefaultMcpLaneWatchdog(
        IMcpServerPreflight preflight,
        IMcpToolRegistry registry,
        IOptions<McpExecutionOptions> options,
        ILogger<DefaultMcpLaneWatchdog> logger)
    {
        _preflight = preflight;
        _registry = registry;
        _options = options;
        _logger = logger;
    }

    public void PerformWatchdogCheck()
    {
        var tools = _registry.ListTools();
        var opt = _options.Value;
        var checkedProfiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tool in tools)
        {
            if (string.IsNullOrEmpty(tool.ServerProfileKey))
                continue;

            // Skip pseudo-profile used only for meta tools.
            if (tool.ServerProfileKey.Equals("mcp-meta", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip if already checked in this watchdog cycle.
            if (!checkedProfiles.Add(tool.ServerProfileKey))
                continue;

            var profileKey = ResolveWatchdogProfileKey(tool, opt);
            var preflightResult = _preflight.CheckServerAvailability(profileKey);
            var snapshot = new McpLaneWatchdogSnapshot
            {
                ProfileKey = profileKey,
                Lane = tool.Lane.ToString(),
                LastCheckTimeUtc = DateTime.UtcNow,
                Status = preflightResult.IsAvailable ? "available" : "degraded",
                BlockerCode = preflightResult.BlockerCode,
                DiagnosticMessage = preflightResult.DiagnosticMessage
            };

            _snapshots.AddOrUpdate(tool.ServerProfileKey, snapshot, (_, _) => snapshot);

            // Add history entry
            var historyEntry = new McpLaneWatchdogHistoryEntry
            {
                CheckTimeUtc = DateTime.UtcNow,
                Status = snapshot.Status,
                BlockerCode = snapshot.BlockerCode
            };

            var profileHistory = _history.GetOrAdd(profileKey, _ => new ConcurrentQueue<McpLaneWatchdogHistoryEntry>());
            profileHistory.Enqueue(historyEntry);

            // Enforce history depth limit
            while (profileHistory.Count > opt.WatchdogHistoryDepth)
            {
                profileHistory.TryDequeue(out _);
            }

            if (!preflightResult.IsAvailable)
            {
                _logger.LogWarning(
                    "MCP lane watchdog: {ProfileKey} ({Lane}) is degraded: {BlockerCode} - {Diagnostic}",
                    tool.ServerProfileKey,
                    tool.Lane,
                    preflightResult.BlockerCode,
                    preflightResult.DiagnosticMessage);
            }
            else
            {
                _logger.LogDebug(
                    "MCP lane watchdog: {ProfileKey} ({Lane}) is available",
                    tool.ServerProfileKey,
                    tool.Lane);
            }
        }
    }

    public IReadOnlyList<McpLaneWatchdogSnapshot> GetSnapshot() =>
        _snapshots.Values.OrderBy(s => s.ProfileKey).ToList();

    public IReadOnlyList<McpLaneWatchdogHistoryEntry> GetHistory(string profileKey) =>
        _history.TryGetValue(profileKey, out var history)
            ? history.ToList()
            : Array.Empty<McpLaneWatchdogHistoryEntry>();

    private static string ResolveWatchdogProfileKey(McpToolMetadata tool, McpExecutionOptions opt)
    {
        if (opt.BrowserLane.UsesObscuraProvider() &&
            tool.Lane == McpExecutionLaneKind.Browser &&
            tool.ServerProfileKey.Equals("obscura-browser-lane", StringComparison.OrdinalIgnoreCase))
        {
            return "obscura-browser-lane";
        }

        return tool.ServerProfileKey;
    }
}
