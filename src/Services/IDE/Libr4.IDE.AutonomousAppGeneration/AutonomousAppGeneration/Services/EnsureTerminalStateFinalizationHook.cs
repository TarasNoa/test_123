using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed class EnsureTerminalStateFinalizationHook : IAutonomousFinalizationHook
{
    public int Order => 100;
    public string Name => "ensure_terminal_state";

    public Task ExecuteAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct)
    {
        if (orchestrator.Status is not GenerationStatus.Completed and not GenerationStatus.Failed)
        {
            orchestrator.MarkFailed("finalization_forced_terminal_state");
        }

        return Task.CompletedTask;
    }
}
