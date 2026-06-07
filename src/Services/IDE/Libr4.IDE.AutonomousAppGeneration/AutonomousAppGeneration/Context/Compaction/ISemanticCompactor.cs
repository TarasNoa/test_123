using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Compaction;

public interface ISemanticCompactor
{
    Task<SemanticCompactionSummary> SummarizeAsync(
        IReadOnlyList<AgentConversationTurn> turnsToSummarize,
        IReadOnlyList<string> manifestPaths,
        CancellationToken ct = default);
}
