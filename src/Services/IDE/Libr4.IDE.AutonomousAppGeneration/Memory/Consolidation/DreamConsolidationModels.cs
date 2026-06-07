namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Consolidation;

public sealed record DreamConsolidationResult(
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc,
    int TotalBefore,
    int EpisodicMergedToSemantic,
    int StalePruned,
    int DuplicatesRemoved,
    int EpisodicRetentionPruned,
    int SemanticAfter,
    bool Success,
    string? ErrorMessage = null);

public interface IDreamConsolidationService
{
    Task<DreamConsolidationResult> RunAsync(CancellationToken ct = default);

    DreamConsolidationResult? GetLastResult();
}
