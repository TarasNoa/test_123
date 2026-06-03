using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface IAgentIntegrationCoordinator
{
    Task OnPlanAttachedAsync(AppGenerationOrchestrator orchestrator, GenerationPlan plan, CancellationToken ct);

    Task IngestGenerationArtifactsAsync(
        AppGenerationOrchestrator orchestrator,
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> files,
        CancellationToken ct);

    Task OnGenerationGatePassedAsync(
        AppGenerationOrchestrator orchestrator,
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> files,
        CancellationToken ct);

    Task OnPostConsistencyAsync(AppGenerationOrchestrator orchestrator, GenerationPlan plan, CancellationToken ct);

    Task OnWorkspaceAttachedAsync(AppGenerationOrchestrator orchestrator, Guid workspaceId, CancellationToken ct);

    Task OnPhaseBuildSucceededAsync(
        AppGenerationOrchestrator orchestrator,
        GenerationPlan plan,
        string phaseName,
        CancellationToken ct);

    Task OnGateFailureAsync(
        AppGenerationOrchestrator orchestrator,
        string stage,
        IReadOnlyList<string> reasons,
        CancellationToken ct);

    Task OnPostFixAsync(AppGenerationOrchestrator orchestrator, GenerationPlan plan, CancellationToken ct);

    Task<SecurityReviewAuditEntry> ReviewGeneratedCodeAsync(
        string stage,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        CancellationToken ct = default);
}
