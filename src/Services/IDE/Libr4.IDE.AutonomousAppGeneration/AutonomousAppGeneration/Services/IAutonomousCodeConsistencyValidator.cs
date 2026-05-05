using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public interface IAutonomousCodeConsistencyValidator
{
    QualityGateResult Validate(IReadOnlyList<GeneratedFile> files, GenerationPlan plan);
}
