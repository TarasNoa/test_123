using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;

public interface IRunUsageRollupService
{
    RunUsageRollup Rollup(Guid runId);
}

public sealed record RunUsageRollup(
    Guid RunId,
    int StepCount,
    int ToolCallCount,
    long InputTokens,
    long OutputTokens,
    long TotalTokens,
    double CostUsd,
    int LlmRequestCount,
    DateTime? LastActivityAtUtc,
    DateTime? LastToolActivityAtUtc);

public sealed class RunUsageRollupService : IRunUsageRollupService
{
    private readonly AgentRuntimeOptions _options;

    public RunUsageRollupService(IOptions<AgentRuntimeOptions> options) => _options = options.Value;

    public RunUsageRollup Rollup(Guid runId)
    {
        var path = Path.Combine(_options.RunsRoot, runId.ToString("D"), "rollout.jsonl");
        if (!File.Exists(path))
        {
            return new RunUsageRollup(
                runId, 0, 0, 0, 0, 0, 0, 0, null, null);
        }

        var stepCount = 0;
        var toolCallCount = 0;
        var llmRequests = 0;
        long inputTokens = 0;
        long outputTokens = 0;
        long totalTokens = 0;
        double costUsd = 0;
        DateTime? lastActivity = null;
        DateTime? lastToolActivity = null;

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
            var timestamp = ReadTimestampUtc(root);

            if (timestamp is not null)
            {
                lastActivity = timestamp;
                if (type is "tool_use")
                    lastToolActivity = timestamp;
            }

            switch (type)
            {
                case "step_start":
                case "step_finish":
                    stepCount++;
                    break;
                case "tool_use":
                    toolCallCount++;
                    break;
            }

            if (type != "step_finish" || !root.TryGetProperty("usage", out var usage) || usage.ValueKind == JsonValueKind.Null)
                continue;

            llmRequests++;
            inputTokens += ReadLong(usage, "inputTokens", "InputTokens");
            outputTokens += ReadLong(usage, "outputTokens", "OutputTokens");
            totalTokens += ReadLong(usage, "totalTokens", "TotalTokens");
            costUsd += ReadDouble(usage, "costUsd", "CostUsd");
        }

        if (totalTokens == 0 && inputTokens + outputTokens > 0)
            totalTokens = inputTokens + outputTokens;

        return new RunUsageRollup(
            runId,
            stepCount,
            toolCallCount,
            inputTokens,
            outputTokens,
            totalTokens,
            costUsd,
            llmRequests,
            lastActivity,
            lastToolActivity);
    }

    private static DateTime? ReadTimestampUtc(JsonElement root)
    {
        if (root.TryGetProperty("timestamp", out var ts) && ts.TryGetInt64(out var ms))
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        return null;
    }

    private static long ReadLong(JsonElement usage, string camel, string pascal)
    {
        if (usage.TryGetProperty(camel, out var v) && v.TryGetInt64(out var n))
            return n;
        if (usage.TryGetProperty(pascal, out var p) && p.TryGetInt64(out var m))
            return m;
        return 0;
    }

    private static double ReadDouble(JsonElement usage, string camel, string pascal)
    {
        if (usage.TryGetProperty(camel, out var v) && v.TryGetDouble(out var n))
            return n;
        if (usage.TryGetProperty(pascal, out var p) && p.TryGetDouble(out var m))
            return m;
        return 0;
    }
}
