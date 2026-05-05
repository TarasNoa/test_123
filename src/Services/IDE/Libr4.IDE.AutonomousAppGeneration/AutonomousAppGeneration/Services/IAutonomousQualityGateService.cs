using Libr4.IDE.Domain.AutonomousAppGeneration;
using Libr4.Shared.Contracts;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed record QualityGateResult(
    string Stage,
    int Score,
    bool Passed,
    IReadOnlyList<string> Reasons,
    ErrorEnvelope? ErrorEnvelope = null);

public interface IAutonomousQualityGateService
{
    QualityGateResult EvaluatePlan(GenerationPlan plan);
    QualityGateResult EvaluateBuild(ExecutionResult execution);
    QualityGateResult EvaluateGeneratedFiles(IReadOnlyList<GeneratedFile> files, GenerationPlan plan);
    QualityGateResult EvaluateExecution(ExecutionResult execution, GenerationPlan plan);
    QualityGateResult EvaluateFixProgress(IReadOnlyList<ErrorReport> errors, IReadOnlyList<GeneratedFile> patches);
}
