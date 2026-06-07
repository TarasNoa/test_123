namespace Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;

public interface IWorkspaceTrustStore
{
    Task EnsureSchemaAsync(CancellationToken ct = default);

    Task<WorkspaceTrustRecord?> GetAsync(string workspaceHash, CancellationToken ct = default);

    Task UpsertAsync(WorkspaceTrustRecord record, CancellationToken ct = default);
}

public sealed class WorkspaceTrustResolution
{
    public required bool NeedsFirstRunPrompt { get; init; }
    public WorkspaceTrustDecision? Decision { get; init; }
    public WorkspaceTrustPrompt? Prompt { get; init; }
}

public interface IWorkspaceTrustService
{
    Task<WorkspaceTrustResolution> ResolveAsync(string workspaceHash, CancellationToken ct = default);

    Task RememberAsync(WorkspaceTrustRecord record, CancellationToken ct = default);
}

public interface IWorkspaceTrustRunGate
{
    Task<WorkspaceTrustRunState> BeginRunAsync(Guid runId, string workspaceHash, CancellationToken ct = default);

    Task WaitForDecisionAsync(Guid runId, CancellationToken ct = default);

    WorkspaceTrustRunState? GetRunState(Guid runId);

    bool DenyCloudInference(Guid runId);

    Task ResolveAsync(Guid runId, WorkspaceTrustResolveRequest request, CancellationToken ct = default);
}
