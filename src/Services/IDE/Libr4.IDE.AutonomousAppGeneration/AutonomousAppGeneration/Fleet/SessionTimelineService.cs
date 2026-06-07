using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public sealed class SessionTimelineService : ISessionTimelineService
{
    private static readonly string[] VerifyEvidenceFiles =
    [
        "readiness.json",
        "verify-report.json",
        "verify-failure-evidence.json",
        "security-scan.json",
        "manifest.json"
    ];

    private readonly AgentFleetOptions _options;
    private readonly IFlowProgressStore _flowProgress;
    private readonly ISubagentStore _subagents;
    private readonly IDelegationManager _delegations;

    public SessionTimelineService(
        IOptions<AgentFleetOptions> options,
        IFlowProgressStore flowProgress,
        ISubagentStore subagents,
        IDelegationManager delegations)
    {
        _options = options.Value;
        _flowProgress = flowProgress;
        _subagents = subagents;
        _delegations = delegations;
    }

    public async Task<SessionTimelineResponse> GetTimelineAsync(Guid runId, CancellationToken ct = default)
    {
        var events = new List<SessionTimelineEvent>();
        var runDir = RunDir(runId);

        events.AddRange(ReadRolloutEvents(runDir));
        events.AddRange(await ReadFlowEventsAsync(runId, ct).ConfigureAwait(false));
        events.AddRange(await ReadSubagentEventsAsync(runId, ct).ConfigureAwait(false));
        events.AddRange(await ReadDelegationEventsAsync(runId, ct).ConfigureAwait(false));
        events.AddRange(ReadVerifyEvents(runDir));

        events.Sort((a, b) => a.TimestampUtc.CompareTo(b.TimestampUtc));
        return new SessionTimelineResponse(runId, events);
    }

    private string RunDir(Guid runId) =>
        Path.Combine(Path.GetFullPath(_options.RunsRoot), runId.ToString("D"));

    private static IEnumerable<SessionTimelineEvent> ReadRolloutEvents(string runDir)
    {
        var path = Path.Combine(runDir, "rollout.jsonl");
        if (!File.Exists(path))
            return Array.Empty<SessionTimelineEvent>();

        var events = new List<SessionTimelineEvent>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            JsonDocument? doc = null;
            try
            {
                doc = JsonDocument.Parse(line);
                var parsed = ParseRolloutLine(doc.RootElement);
                if (parsed is not null)
                    events.Add(parsed);
            }
            catch (JsonException)
            {
                // skip malformed rollout line
            }
            finally
            {
                doc?.Dispose();
            }
        }

        return events;
    }

    private static SessionTimelineEvent? ParseRolloutLine(JsonElement root)
    {
        if (!root.TryGetProperty("type", out var typeProp))
            return null;

        var type = typeProp.GetString();
        var timestamp = ReadTimestamp(root);
        var stepNumber = root.TryGetProperty("stepNumber", out var step) ? step.GetInt32() : (int?)null;

        return type switch
        {
            "tool_use" => new SessionTimelineEvent(
                SessionTimelineKind.ToolCall,
                timestamp,
                root.TryGetProperty("toolName", out var tn) ? tn.GetString() ?? "tool" : "tool",
                Truncate(ExtractOutputPreview(root)),
                root.TryGetProperty("success", out var ok) && ok.GetBoolean(),
                stepNumber,
                root.TryGetProperty("sessionId", out var sid) ? sid.GetString() : null),

            "step_start" => new SessionTimelineEvent(
                SessionTimelineKind.StepStart,
                timestamp,
                "Agent step started",
                stepNumber is null ? null : $"Step {stepNumber}",
                null,
                stepNumber,
                root.TryGetProperty("sessionId", out var ss) ? ss.GetString() : null),

            "step_finish" => new SessionTimelineEvent(
                SessionTimelineKind.StepFinish,
                timestamp,
                "Agent step finished",
                root.TryGetProperty("finishReason", out var fr) ? fr.GetString() : null,
                null,
                stepNumber,
                root.TryGetProperty("sessionId", out var sf) ? sf.GetString() : null),

            "error" => new SessionTimelineEvent(
                SessionTimelineKind.Error,
                timestamp,
                "Error",
                root.TryGetProperty("message", out var msg) ? Truncate(msg.GetString()) : null,
                false,
                stepNumber,
                root.TryGetProperty("sessionId", out var es) ? es.GetString() : null),

            "permission" => new SessionTimelineEvent(
                SessionTimelineKind.Permission,
                timestamp,
                $"Permission: {(root.TryGetProperty("toolName", out var pt) ? pt.GetString() : null) ?? "tool"}",
                root.TryGetProperty("decision", out var dec) ? dec.GetString() : null,
                string.Equals(
                    root.TryGetProperty("decision", out var dec2) ? dec2.GetString() : null,
                    "allow",
                    StringComparison.OrdinalIgnoreCase),
                null,
                null),

            "obscura_execpolicy_prompt" => new SessionTimelineEvent(
                SessionTimelineKind.ExecPolicyConsent,
                timestamp,
                "Obscura exec policy consent",
                BuildExecPolicyDetail(root),
                null,
                stepNumber,
                root.TryGetProperty("promptId", out var pid) ? pid.GetString() : null),

            _ => null
        };
    }

    private static string? BuildExecPolicyDetail(JsonElement root)
    {
        var tool = root.TryGetProperty("toolName", out var tn) ? tn.GetString() : null;
        var target = root.TryGetProperty("target", out var tg) ? tg.GetString() : null;
        var reason = root.TryGetProperty("reason", out var rs) ? rs.GetString() : null;
        return string.Join(" | ", new[] { tool, target, reason }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private async Task<IEnumerable<SessionTimelineEvent>> ReadFlowEventsAsync(Guid runId, CancellationToken ct)
    {
        var flow = await _flowProgress.LoadAsync(runId, ct).ConfigureAwait(false);
        if (flow is null)
            return Array.Empty<SessionTimelineEvent>();

        var events = new List<SessionTimelineEvent>();
        foreach (var node in flow.Nodes)
        {
            if (string.Equals(node.Status, "pending", StringComparison.OrdinalIgnoreCase))
                continue;

            var success = string.Equals(node.Status, "completed", StringComparison.OrdinalIgnoreCase)
                ? true
                : string.Equals(node.Status, "failed", StringComparison.OrdinalIgnoreCase)
                    ? false
                    : (bool?)null;

            events.Add(new SessionTimelineEvent(
                SessionTimelineKind.FlowNode,
                flow.UpdatedAtUtc,
                $"Flow node: {node.NodeId}",
                node.LastError ?? node.Status,
                success,
                node.Attempts > 0 ? node.Attempts : null,
                flow.FlowName));

            if (node.Attempts > 0)
            {
                events.Add(new SessionTimelineEvent(
                    SessionTimelineKind.Phase,
                    flow.UpdatedAtUtc,
                    $"Phase {node.NodeId}",
                    $"attempts={node.Attempts} status={node.Status}",
                    success,
                    null,
                    flow.FlowName));
            }
        }

        return events;
    }

    private async Task<IEnumerable<SessionTimelineEvent>> ReadSubagentEventsAsync(Guid runId, CancellationToken ct)
    {
        var records = await _subagents.ListAsync(runId, ct).ConfigureAwait(false);
        var events = new List<SessionTimelineEvent>(records.Count * 2);

        foreach (var record in records)
        {
            events.Add(new SessionTimelineEvent(
                SessionTimelineKind.SubagentSpawn,
                record.CreatedAtUtc,
                $"Subagent spawned: {record.Name}",
                Truncate(record.Task),
                null,
                null,
                record.Id));

            if (string.Equals(record.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                events.Add(new SessionTimelineEvent(
                    SessionTimelineKind.SubagentComplete,
                    record.UpdatedAtUtc,
                    $"Subagent completed: {record.Name}",
                    Truncate(record.OutputPreview),
                    true,
                    null,
                    record.Id));
            }
            else if (string.Equals(record.Status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                events.Add(new SessionTimelineEvent(
                    SessionTimelineKind.SubagentComplete,
                    record.UpdatedAtUtc,
                    $"Subagent failed: {record.Name}",
                    Truncate(record.Error ?? record.OutputPreview),
                    false,
                    null,
                    record.Id));
            }
        }

        return events;
    }

    private async Task<IEnumerable<SessionTimelineEvent>> ReadDelegationEventsAsync(Guid runId, CancellationToken ct)
    {
        var records = await _delegations.ListAsync(runId, ct).ConfigureAwait(false);
        var events = new List<SessionTimelineEvent>(records.Count * 2);

        foreach (var record in records)
        {
            events.Add(new SessionTimelineEvent(
                SessionTimelineKind.DelegationStart,
                record.CreatedAtUtc,
                "Delegation started",
                Truncate(record.Task),
                null,
                null,
                record.Id));

            if (string.Equals(record.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                events.Add(new SessionTimelineEvent(
                    SessionTimelineKind.DelegationComplete,
                    record.UpdatedAtUtc,
                    "Delegation completed",
                    Truncate(record.OutputPreview),
                    true,
                    null,
                    record.Id));
            }
            else if (string.Equals(record.Status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                events.Add(new SessionTimelineEvent(
                    SessionTimelineKind.DelegationComplete,
                    record.UpdatedAtUtc,
                    "Delegation failed",
                    Truncate(record.Error ?? record.OutputPreview),
                    false,
                    null,
                    record.Id));
            }
        }

        return events;
    }

    private static IEnumerable<SessionTimelineEvent> ReadVerifyEvents(string runDir)
    {
        var verifyDir = Path.Combine(runDir, "verify");
        if (!Directory.Exists(verifyDir))
            yield break;

        foreach (var fileName in VerifyEvidenceFiles)
        {
            var path = Path.Combine(verifyDir, fileName);
            if (!File.Exists(path))
                continue;

            var timestamp = File.GetLastWriteTimeUtc(path);
            var passed = TryReadVerifyPassed(path);
            yield return new SessionTimelineEvent(
                SessionTimelineKind.VerifyAttempt,
                timestamp,
                $"Verify: {fileName}",
                passed switch
                {
                    true => "passed",
                    false => "failed",
                    _ => "recorded"
                },
                passed,
                null,
                fileName);
        }
    }

    private static bool? TryReadVerifyPassed(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (root.TryGetProperty("passed", out var passed))
                return passed.GetBoolean();
            if (root.TryGetProperty("ready", out var ready))
                return ready.GetBoolean();
            if (root.TryGetProperty("status", out var status))
            {
                var value = status.GetString();
                if (string.Equals(value, "pass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "passed", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(value, "fail", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "failed", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }
        catch
        {
            // ignore malformed evidence
        }

        return null;
    }

    private static DateTime ReadTimestamp(JsonElement root)
    {
        if (root.TryGetProperty("timestamp", out var ts))
        {
            if (ts.ValueKind == JsonValueKind.Number)
                return DateTimeOffset.FromUnixTimeMilliseconds(ts.GetInt64()).UtcDateTime;
            if (ts.ValueKind == JsonValueKind.String && DateTime.TryParse(ts.GetString(), out var parsed))
                return parsed.ToUniversalTime();
        }

        return DateTime.UtcNow;
    }

    private static string? ExtractOutputPreview(JsonElement root)
    {
        if (!root.TryGetProperty("outputJson", out var output))
            return null;
        return Truncate(output.GetString());
    }

    private static string? Truncate(string? text, int max = 240) =>
        string.IsNullOrWhiteSpace(text) ? null
        : text.Length <= max ? text
        : text[..max] + "…";
}
