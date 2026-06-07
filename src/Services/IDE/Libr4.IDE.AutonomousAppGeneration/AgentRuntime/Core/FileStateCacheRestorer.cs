using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Persistence;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

public static class FileStateCacheRestorer
{
    public static void RestoreFromToolCalls(IFileStateCache fileState, IEnumerable<AgentToolCallRecord> toolCalls)
    {
        foreach (var call in toolCalls.Where(c => c.Success))
        {
            if (!string.Equals(call.ToolName, "read_file", StringComparison.OrdinalIgnoreCase))
                continue;

            var path = TryGetPath(call.InputJson);
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var normalized = FixerPatchScopePolicy.NormalizePatchRelativePath(path);
            if (!fileState.HasRead(normalized))
                fileState.RecordRead(normalized, call.OutputJson ?? string.Empty, call.StartedAtUtc);
        }
    }

    private static string? TryGetPath(string inputJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(inputJson);
            if (doc.RootElement.TryGetProperty("path", out var path) && path.ValueKind == JsonValueKind.String)
                return path.GetString();
        }
        catch
        {
            // ignore malformed persisted input
        }

        return null;
    }
}
