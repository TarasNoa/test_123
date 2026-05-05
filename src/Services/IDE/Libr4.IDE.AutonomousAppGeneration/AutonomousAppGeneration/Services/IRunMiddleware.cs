using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public interface IRunMiddleware
{
    int Order { get; }
    string Name { get; }

    Task OnBeforeStageAsync(AppGenerationOrchestrator orchestrator, string stage, CancellationToken ct);

    Task OnAfterStageAsync(
        AppGenerationOrchestrator orchestrator,
        string stage,
        bool succeeded,
        string? detail,
        CancellationToken ct);
}
