using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Handlers;

public sealed class GetStageCReadinessQueryHandler
    : IRequestHandler<GetStageCReadinessQuery, StageCReadinessDto>
{
    private readonly IMcpLaneWatchdog _watchdog;
    private readonly IMcpToolRegistry _registry;
    private readonly IOptions<McpExecutionOptions> _mcpOptions;

    public GetStageCReadinessQueryHandler(
        IMcpLaneWatchdog watchdog,
        IMcpToolRegistry registry,
        IOptions<McpExecutionOptions> mcpOptions)
    {
        _watchdog = watchdog;
        _registry = registry;
        _mcpOptions = mcpOptions;
    }

    public Task<StageCReadinessDto> Handle(GetStageCReadinessQuery request, CancellationToken ct)
    {
        _watchdog.PerformWatchdogCheck();
        var snapshot = _watchdog.GetSnapshot();
        var options = _mcpOptions.Value;

        var laneByProfile = _registry.ListTools()
            .Where(t => !string.IsNullOrWhiteSpace(t.ServerProfileKey))
            .GroupBy(t => t.ServerProfileKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Lane.ToString()).FirstOrDefault() ?? "Internal",
                StringComparer.OrdinalIgnoreCase);

        var items = snapshot
            .Select(s => new StageCReadinessItemDto(
                ProfileKey: s.ProfileKey,
                Lane: laneByProfile.TryGetValue(s.ProfileKey, out var lane) ? lane : s.Lane,
                Status: s.Status,
                BlockerCode: s.BlockerCode,
                DiagnosticMessage: s.DiagnosticMessage,
                KillSwitchActive: IsKillSwitchActive(
                    laneByProfile.TryGetValue(s.ProfileKey, out var laneName) ? laneName : s.Lane,
                    options),
                RemediationHints: BuildRemediationHints(
                    laneByProfile.TryGetValue(s.ProfileKey, out var laneLabel) ? laneLabel : s.Lane,
                    s.BlockerCode,
                    s.Status)))
            .OrderBy(i => i.ProfileKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var degraded = items.Count(i => i.Status.Equals("degraded", StringComparison.OrdinalIgnoreCase));
        var overall = degraded == 0 ? "ready" : "degraded";
        var recommendations = BuildOverallRecommendations(items, options);

        var dto = new StageCReadinessDto(
            GeneratedAtUtc: DateTime.UtcNow,
            DeterministicFallbackEnabled: options.EnableDeterministicFallback,
            StdioTransportEnabled: options.EnableStdioTransport,
            TotalProfiles: items.Count,
            DegradedProfiles: degraded,
            OverallStatus: overall,
            OverallRecommendations: recommendations,
            Items: items);

        return Task.FromResult(dto);
    }

    private static bool IsKillSwitchActive(string lane, McpExecutionOptions options) =>
        lane.Equals("Browser", StringComparison.OrdinalIgnoreCase)
            ? options.KillSwitchBrowserLane
            : lane.Equals("N8n", StringComparison.OrdinalIgnoreCase)
                ? options.KillSwitchN8nLane
                : lane.Equals("Workflow", StringComparison.OrdinalIgnoreCase)
                    ? options.KillSwitchWorkflowLane
                    : false;

    private static IReadOnlyList<string> BuildRemediationHints(string lane, string? blockerCode, string status)
    {
        var hints = new List<string>();
        if (status.Equals("available", StringComparison.OrdinalIgnoreCase))
        {
            hints.Add("No action required for this profile.");
            return hints;
        }

        if (string.Equals(blockerCode, "mcp_server_missing", StringComparison.OrdinalIgnoreCase))
        {
            hints.Add($"Verify configured executable/script path for {lane} lane server.");
            hints.Add("Install or create local MCP server instance at configured location.");
        }
        else if (string.Equals(blockerCode, "mcp_server_unreachable", StringComparison.OrdinalIgnoreCase))
        {
            hints.Add($"Start/repair {lane} lane MCP server process and re-run readiness check.");
            hints.Add("Validate server binary permissions and startup command.");
        }
        else
        {
            hints.Add("Inspect MCP lane diagnostics and server startup logs.");
        }

        hints.Add("Recheck endpoint /api/ide/app-generation/dashboard/readiness after fix.");
        return hints;
    }

    private static IReadOnlyList<string> BuildOverallRecommendations(
        IReadOnlyList<StageCReadinessItemDto> items,
        McpExecutionOptions options)
    {
        var recommendations = new List<string>();
        if (!options.EnableStdioTransport)
            recommendations.Add("EnableStdioTransport must be true for real MCP execution lanes.");
        if (options.EnableDeterministicFallback)
            recommendations.Add("Deterministic fallback is enabled; degraded lanes will not hard-fail runs.");

        foreach (var item in items.Where(i => i.Status.Equals("degraded", StringComparison.OrdinalIgnoreCase)))
            recommendations.Add($"Fix lane/profile '{item.Lane}/{item.ProfileKey}' blocker '{item.BlockerCode ?? "unknown"}'.");

        if (recommendations.Count == 0)
            recommendations.Add("All Stage C readiness checks are green.");

        return recommendations.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
