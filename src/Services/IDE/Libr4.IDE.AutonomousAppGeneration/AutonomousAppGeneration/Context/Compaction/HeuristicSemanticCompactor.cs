using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Compaction;

/// <summary>Deterministic semantic extractor — no LLM, safe for tests and fallback.</summary>
public sealed class HeuristicSemanticCompactor : ISemanticCompactor
{
    public Task<SemanticCompactionSummary> SummarizeAsync(
        IReadOnlyList<AgentConversationTurn> turnsToSummarize,
        IReadOnlyList<string> manifestPaths,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(FSharpAlgorithmsBridge.SummarizeConversation(turnsToSummarize, manifestPaths));
    }
}
