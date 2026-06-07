namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public interface ISpaceOrchestrator
{
    Task<SpaceOrchestrationResult> RunParallelPipelineAsync(
        Guid spaceId,
        SpaceOrchestrationRequest request,
        CancellationToken ct = default);

    Task SignalContextReadyAsync(
        Guid spaceId,
        string memberId,
        string kind,
        string title,
        string? payload,
        CancellationToken ct = default);
}
