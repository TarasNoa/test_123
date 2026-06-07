using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FineTuning;

public sealed record FineTuningExample(
    string Instruction,
    string Output,
    string Stack,
    Guid RunId,
    DateTime CreatedAtUtc);

public sealed record FineTuningQualityReport(
    bool Passed,
    double ReadabilityScore,
    bool SyntaxValid,
    bool Duplicate,
    string? RejectionReason);

public sealed record FineTuningExportResult(
    Guid RunId,
    string Stack,
    bool Accepted,
    string? DatasetPath,
    FineTuningQualityReport Quality);

public sealed record FineTuningDatasetBuildResult(
    int RunsProcessed,
    int ExamplesAccepted,
    int ExamplesRejected,
    IReadOnlyDictionary<string, int> PerStackCounts);

public interface IFineTuningDataPipelineService
{
    Task<FineTuningExportResult> ExportRunAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default);

    Task<FineTuningDatasetBuildResult> BuildDatasetAsync(
        IEnumerable<AppGenerationOrchestrator> runs,
        CancellationToken ct = default);
}
