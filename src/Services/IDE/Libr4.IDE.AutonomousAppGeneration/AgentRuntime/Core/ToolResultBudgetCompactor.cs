using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

public sealed class ToolResultBudgetCompactor : IContextCompactor
{
    private readonly AgentRuntimeOptions _options;

    public ToolResultBudgetCompactor(IOptions<AgentRuntimeOptions> options)
    {
        _options = options.Value;
    }

    public Task<IReadOnlyList<AgentConversationTurn>> CompactAsync(
        IReadOnlyList<AgentConversationTurn> turns,
        int charBudget,
        Context.Compaction.CompactionRequest? request = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(CompactSync(turns, charBudget));
    }

    private IReadOnlyList<AgentConversationTurn> CompactSync(IReadOnlyList<AgentConversationTurn> turns, int charBudget)
    {
        if (turns.Count == 0)
            return turns;

        var budget = charBudget > 0 ? charBudget : _options.ConversationCharBudget;
        var total = turns.Sum(t => t.Content.Length);
        if (total <= budget)
            return turns;

        var kept = new List<AgentConversationTurn>();
        if (turns.Count > 0)
            kept.Add(turns[0]);

        var tail = turns.Skip(Math.Max(1, turns.Count - 12)).ToList();
        foreach (var turn in tail)
        {
            if (turn.Role.Equals("tool", StringComparison.OrdinalIgnoreCase) && turn.Content.Length > _options.MaxToolResultChars)
            {
                kept.Add(new AgentConversationTurn(
                    turn.Role,
                    turn.Content[.._options.MaxToolResultChars] + "\n...[truncated]...",
                    turn.AtUtc));
            }
            else
            {
                kept.Add(turn);
            }
        }

        if (kept.Count < turns.Count)
        {
            kept.Insert(1, new AgentConversationTurn(
                "system",
                $"[context compacted: dropped {turns.Count - kept.Count} middle turns to stay within {budget} chars]",
                DateTime.UtcNow));
        }

        return kept;
    }
}
