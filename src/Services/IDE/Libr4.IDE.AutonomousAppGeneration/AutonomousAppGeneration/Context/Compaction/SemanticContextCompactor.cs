using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Compaction;

public sealed class SemanticContextCompactor : IContextCompactor
{
    private readonly ISemanticCompactor _semantic;
    private readonly ToolResultBudgetCompactor _truncateFallback;
    private readonly SemanticCompactionOptions _options;
    private readonly AgentRuntimeOptions _runtimeOptions;
    private readonly IAgentLifecycleHookRunner? _lifecycle;
    private readonly IRolloutRecorder? _rollout;

    public SemanticContextCompactor(
        ISemanticCompactor semantic,
        IOptions<SemanticCompactionOptions> options,
        IOptions<AgentRuntimeOptions> runtimeOptions,
        IAgentLifecycleHookRunner? lifecycle = null,
        IRolloutRecorder? rollout = null)
    {
        _semantic = semantic;
        _truncateFallback = new ToolResultBudgetCompactor(runtimeOptions);
        _options = options.Value;
        _runtimeOptions = runtimeOptions.Value;
        _lifecycle = lifecycle;
        _rollout = rollout;
    }

    public async Task<IReadOnlyList<AgentConversationTurn>> CompactAsync(
        IReadOnlyList<AgentConversationTurn> turns,
        int charBudget,
        CompactionRequest? request = null,
        CancellationToken ct = default)
    {
        if (turns.Count == 0)
            return turns;

        var budget = charBudget > 0 ? charBudget : _runtimeOptions.ConversationCharBudget;
        var total = turns.Sum(t => t.Content.Length);
        var trigger = (int)(budget * _options.TriggerBudgetRatio);

        if (!_options.EnableSemanticCompaction
            || total <= trigger
            || turns.Count < _options.MinTurnsBeforeCompaction)
            return await _truncateFallback.CompactAsync(turns, budget, request, ct).ConfigureAwait(false);

        if (_lifecycle is not null)
        {
            await _lifecycle.RunAsync(AgentHookKind.PreCompact, new HookContext
            {
                RunId = request?.RunId,
                SessionId = request?.SessionId,
                RequestFingerprint = request?.RequestFingerprint,
                Stage = request?.Stage ?? "compact"
            }, ct).ConfigureAwait(false);
        }

        var manifest = request?.ManifestFiles ?? Array.Empty<string>();
        var head = turns[0];
        var tailStart = Math.Max(1, turns.Count - 12);
        var middle = turns.Skip(1).Take(tailStart - 1).ToList();
        var tail = turns.Skip(tailStart).ToList();

        var preservedTools = tail
            .Where(IsToolTurn)
            .Reverse()
            .Take(_options.PreserveLastToolResults)
            .Reverse()
            .ToList();
        var preservedToolBodies = preservedTools
            .Select(t => t.Content)
            .ToHashSet(StringComparer.Ordinal);

        var preservedErrors = turns
            .Where(t => IsErrorTurn(t) || ContainsManifestPath(t, manifest))
            .DistinctBy(t => t.Content, StringComparer.Ordinal)
            .Take(8)
            .ToList();
        var preservedErrorBodies = preservedErrors
            .Select(t => t.Content)
            .ToHashSet(StringComparer.Ordinal);

        var middleToSummarize = middle
            .Where(t => !preservedErrorBodies.Contains(t.Content) && !preservedToolBodies.Contains(t.Content))
            .ToList();

        var summary = await _semantic.SummarizeAsync(middleToSummarize, manifest, ct).ConfigureAwait(false);
        var summaryTurn = new AgentConversationTurn(
            "system",
            summary.ToPromptBlock(),
            DateTime.UtcNow);

        var rebuilt = new List<AgentConversationTurn> { head, summaryTurn };
        rebuilt.AddRange(preservedErrors.Where(t => t.Content != head.Content));
        rebuilt.AddRange(preservedTools);
        rebuilt.AddRange(tail.Where(t => !preservedToolBodies.Contains(t.Content)));

        IReadOnlyList<AgentConversationTurn> result = DeduplicateAdjacent(rebuilt);
        var afterChars = result.Sum(t => t.Content.Length);
        if (afterChars > budget)
            result = await _truncateFallback.CompactAsync(result, budget, request, ct).ConfigureAwait(false);

        if (request?.RunId is Guid runId && _rollout is not null && _runtimeOptions.EnableRolloutRecorder)
        {
            await _rollout.RecordCompactionAsync(
                runId,
                request.SessionId ?? "unknown",
                total,
                result.Sum(t => t.Content.Length),
                turns.Count,
                result.Count,
                summary.ToJson(),
                ct).ConfigureAwait(false);
        }

        if (_lifecycle is not null)
        {
            await _lifecycle.RunAsync(AgentHookKind.PostCompact, new HookContext
            {
                RunId = request?.RunId,
                SessionId = request?.SessionId
            }, ct).ConfigureAwait(false);
        }

        return result;
    }

    private static bool IsToolTurn(AgentConversationTurn turn) =>
        turn.Role.Equals("tool", StringComparison.OrdinalIgnoreCase);

    private static bool IsErrorTurn(AgentConversationTurn turn) =>
        turn.Content.Contains("error", StringComparison.OrdinalIgnoreCase)
        || turn.Content.Contains("failed", StringComparison.OrdinalIgnoreCase)
        || turn.Content.Contains("rejected", StringComparison.OrdinalIgnoreCase)
        || turn.Content.Contains("ModuleNotFound", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsManifestPath(AgentConversationTurn turn, IReadOnlyList<string> manifest)
    {
        if (manifest.Count == 0)
            return false;
        return manifest.Any(path =>
            !string.IsNullOrWhiteSpace(path)
            && turn.Content.Contains(path, StringComparison.OrdinalIgnoreCase));
    }

    private static List<AgentConversationTurn> DeduplicateAdjacent(IReadOnlyList<AgentConversationTurn> turns)
    {
        var list = new List<AgentConversationTurn>(turns.Count);
        AgentConversationTurn? prev = null;
        foreach (var turn in turns)
        {
            if (prev is not null
                && prev.Role == turn.Role
                && prev.Content == turn.Content)
                continue;
            list.Add(turn);
            prev = turn;
        }

        return list;
    }
}
