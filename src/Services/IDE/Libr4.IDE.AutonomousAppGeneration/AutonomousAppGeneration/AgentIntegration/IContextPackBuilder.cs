using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface IContextPackBuilder
{
    Task<string> BuildPackAsync(
        string stage,
        AppGenerationOrchestrator orchestrator,
        int maxChars,
        CancellationToken ct = default);
}
