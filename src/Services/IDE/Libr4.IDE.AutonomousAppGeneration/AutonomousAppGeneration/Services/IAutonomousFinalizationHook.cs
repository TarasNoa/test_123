using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public interface IAutonomousFinalizationHook
{
    int Order { get; }
    string Name { get; }

    Task ExecuteAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct);
}
