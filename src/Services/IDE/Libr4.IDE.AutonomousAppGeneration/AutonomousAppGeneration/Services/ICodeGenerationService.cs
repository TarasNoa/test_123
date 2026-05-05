using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed record GenerationPhaseBatchResult(
    string PhaseName,
    IReadOnlyList<GeneratedFile> Files);

/// <summary>
/// Produces the initial set of source files for the application (phase 1)
/// and also applies fixer patches based on <see cref="ErrorReport"/>s during
/// subsequent iterations.
/// </summary>
public interface ICodeGenerationService
{
    /// <summary>Generates the initial project files based on the plan.</summary>
    Task<IReadOnlyList<GeneratedFile>> GenerateInitialAsync(
        GenerationPlan plan, CancellationToken ct = default);

    /// <summary>
    /// Generates initial project files grouped by strict phases to support
    /// fail-fast compile checks after each phase.
    /// </summary>
    Task<IReadOnlyList<GenerationPhaseBatchResult>> GenerateInitialByPhasesAsync(
        GenerationPlan plan, CancellationToken ct = default);

    /// <summary>Applies fixes for the supplied error reports on top of the existing files.</summary>
    Task<IReadOnlyList<GeneratedFile>> ApplyFixesAsync(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> currentFiles,
        IReadOnlyList<ErrorReport> errors,
        CancellationToken ct = default);
}
