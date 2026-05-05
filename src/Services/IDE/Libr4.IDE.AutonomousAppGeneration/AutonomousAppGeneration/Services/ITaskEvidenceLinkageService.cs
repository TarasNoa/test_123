using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Evidence link connecting a task to its execution artifacts.
/// </summary>
public sealed record TaskEvidenceLink(
    string TaskId,
    IReadOnlyList<string> ChangedFilePaths,
    IReadOnlyList<string> ExecutedCommands,
    IReadOnlyList<string> QualityGateReferences,
    DateTime LinkedAtUtc);

/// <summary>
/// Task evidence manifest for traceability and audit.
/// </summary>
public sealed record TaskEvidenceManifest(
    string TaskId,
    string Stage,
    int TotalLinks,
    int FilesChanged,
    int CommandsExecuted,
    int GatesReferenced,
    bool HasCompleteEvidence);

/// <summary>
/// Service for establishing and validating strict linkage between tasks and their evidence.
/// Ensures each planned/recovery task can be traced to changed files, executed tests/commands, and quality gate evidence.
/// </summary>
public interface ITaskEvidenceLinkageService
{
    /// <summary>
    /// Create evidence link for a task based on execution results.
    /// </summary>
    TaskEvidenceLink LinkTaskToEvidence(
        AgentTaskGraphEntry task,
        IReadOnlyList<string> changedFiles,
        IReadOnlyList<string> executedCommands,
        IReadOnlyList<string> gateReferences);

    /// <summary>
    /// Generate evidence manifest for a task showing linkage completeness.
    /// </summary>
    TaskEvidenceManifest GenerateManifest(
        AgentTaskGraphEntry task,
        IReadOnlyList<TaskEvidenceLink> links);

    /// <summary>
    /// Validate that a task has sufficient evidence linkage.
    /// </summary>
    bool ValidateEvidenceLinkage(
        AgentTaskGraphEntry task,
        IReadOnlyList<TaskEvidenceLink> links);

    /// <summary>
    /// Get all evidence links for a task graph.
    /// </summary>
    IReadOnlyList<TaskEvidenceLink> GetLinksForGraph(
        IReadOnlyList<AgentTaskGraphEntry> graph,
        IReadOnlyList<TaskEvidenceLink> allLinks);
}
