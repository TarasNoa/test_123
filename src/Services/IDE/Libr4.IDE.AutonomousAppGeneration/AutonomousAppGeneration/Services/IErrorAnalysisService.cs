using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Takes the console output of a failing run and produces structured
/// <see cref="ErrorReport"/>s. Typically backed by the existing
/// SemanticBlame agent + an LLM call on OpenRouter.
/// </summary>
public interface IErrorAnalysisService
{
    Task<IReadOnlyList<ErrorReport>> AnalyzeAsync(
        ExecutionResult execution,
        IReadOnlyList<GeneratedFile> files,
        CancellationToken ct = default);
}
