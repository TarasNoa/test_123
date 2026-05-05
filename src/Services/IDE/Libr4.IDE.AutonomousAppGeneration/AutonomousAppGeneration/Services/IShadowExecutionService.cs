using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Executes the generated code inside an isolated shadow workspace
/// (container or sandboxed process), captures stdout/stderr and returns
/// a structured <see cref="ExecutionResult"/> that the orchestrator can hand
/// over to the fixer agent.
/// </summary>
public interface IShadowExecutionService
{
    /// <summary>Materializes files on disk/workspace and returns its id.</summary>
    Task<Guid> PrepareWorkspaceAsync(
        IReadOnlyList<GeneratedFile> files,
        string runtimeImage,
        CancellationToken ct = default);

    /// <summary>Updates files inside an existing workspace.</summary>
    Task UpdateWorkspaceAsync(
        Guid workspaceId,
        IReadOnlyList<GeneratedFile> files,
        CancellationToken ct = default);

    /// <summary>Runs build + tests inside the workspace and returns the execution result.</summary>
    Task<ExecutionResult> RunAsync(
        Guid workspaceId, GenerationPlan plan, CancellationToken ct = default);

    /// <summary>Disposes the workspace (container, temp dirs, etc.).</summary>
    Task DisposeWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
}
