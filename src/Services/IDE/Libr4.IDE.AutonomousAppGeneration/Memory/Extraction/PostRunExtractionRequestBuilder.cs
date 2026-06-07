using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;

public sealed class PostRunExtractionRequestBuilder
{
    private readonly IRolloutReplayService? _rollout;
    private readonly PostRunExtractionOptions _options;

    public PostRunExtractionRequestBuilder(
        IOptions<PostRunExtractionOptions> options,
        IRolloutReplayService? rollout = null)
    {
        _options = options.Value;
        _rollout = rollout;
    }

    public async Task<PostRunExtractionRequest> BuildAsync(
        AppGenerationOrchestrator orchestrator,
        CancellationToken ct = default)
    {
        var rolloutLines = await BuildRolloutLinesAsync(orchestrator.Id, ct).ConfigureAwait(false);
        var errors = orchestrator.Iterations
            .SelectMany(i => i.Errors)
            .Take(24)
            .ToArray();

        return new PostRunExtractionRequest(
            orchestrator.Id,
            orchestrator.Status,
            orchestrator.RequestFingerprint,
            orchestrator.FailureReason,
            orchestrator.Plan?.ApplicationName,
            RepairPlaybookSignature.BuildStackPattern(orchestrator.Plan),
            rolloutLines,
            errors,
            orchestrator.Iterations.Count);
    }

    private async Task<IReadOnlyList<string>> BuildRolloutLinesAsync(Guid runId, CancellationToken ct)
    {
        if (_rollout is null)
            return Array.Empty<string>();

        var entries = await _rollout.ReplayAsync(runId, ct).ConfigureAwait(false);
        return entries
            .Where(e => e.Type is "tool_use" or "error" or "step_finish" or "compaction" or "memory_operation")
            .Take(_options.MaxRolloutLines)
            .Select(FormatRolloutLine)
            .ToArray();
    }

    private string FormatRolloutLine(RolloutEntry entry)
    {
        try
        {
            using var doc = JsonDocument.Parse(entry.PayloadJson);
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var t) ? t.GetString() : entry.Type;
            var tool = root.TryGetProperty("toolName", out var tn) ? tn.GetString() : string.Empty;
            var success = root.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True;
            var output = root.TryGetProperty("outputJson", out var o) ? o.GetString() : string.Empty;
            var finish = root.TryGetProperty("finishReason", out var f) ? f.GetString() : string.Empty;
            var line = $"[{type}] step={entry.StepNumber} tool={tool} success={success} finish={finish} output={Truncate(output ?? entry.PayloadJson, Math.Min(400, _options.MaxRolloutCharsPerLine))}";
            return Truncate(line, _options.MaxRolloutCharsPerLine);
        }
        catch
        {
            return Truncate($"[{entry.Type}] step={entry.StepNumber} {entry.PayloadJson}", _options.MaxRolloutCharsPerLine);
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";
}
