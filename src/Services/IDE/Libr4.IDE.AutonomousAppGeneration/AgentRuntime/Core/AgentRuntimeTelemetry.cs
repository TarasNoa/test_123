using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

public static class AgentRuntimeTelemetry
{
    public static string FormatSummary(AgentSessionMode mode, AgentSessionResult result)
    {
        var toolCounts = result.Trace
            .Where(t => t.StartsWith("turn_", StringComparison.Ordinal) && t.Contains(":tool:", StringComparison.Ordinal))
            .Select(t =>
            {
                var parts = t.Split(':');
                return parts.Length >= 4 ? parts[3] : "unknown";
            })
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}={g.Count()}")
            .ToList();

        var tools = toolCounts.Count == 0 ? "none" : string.Join(", ", toolCounts);
        return $"mode={mode} turns={result.TurnsUsed} patches={result.Patches.Count} succeeded={result.Succeeded} tools=[{tools}] summary={result.Summary}";
    }
}
