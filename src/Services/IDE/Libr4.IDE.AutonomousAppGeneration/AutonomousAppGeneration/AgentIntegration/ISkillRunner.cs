using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface ISkillRunner
{
    Task RecordStageSelectionAsync(
        AppGenerationOrchestrator orchestrator,
        string stage,
        GenerationPlan? plan,
        CancellationToken ct);
}
