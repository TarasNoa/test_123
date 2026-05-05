using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Service for establishing strict linkage between tasks and their execution evidence.
/// Ensures complete traceability for audit and debugging.
/// </summary>
public sealed class TaskEvidenceLinkageService : ITaskEvidenceLinkageService
{
    private readonly ILogger<TaskEvidenceLinkageService> _logger;
    private const int MinFilesChangedForEvidence = 0; // Recovery tasks may not change files
    private const int MinCommandsExecutedForEvidence = 1; // At least one command should be executed

    public TaskEvidenceLinkageService(ILogger<TaskEvidenceLinkageService> logger)
    {
        _logger = logger;
    }

    public TaskEvidenceLink LinkTaskToEvidence(
        AgentTaskGraphEntry task,
        IReadOnlyList<string> changedFiles,
        IReadOnlyList<string> executedCommands,
        IReadOnlyList<string> gateReferences)
    {
        var link = new TaskEvidenceLink(
            task.TaskId,
            changedFiles,
            executedCommands,
            gateReferences,
            DateTime.UtcNow);

        _logger.LogInformation(
            "Linked task {TaskId} to evidence: {Files} files, {Commands} commands, {Gates} gate references",
            task.TaskId, changedFiles.Count, executedCommands.Count, gateReferences.Count);

        return link;
    }

    public TaskEvidenceManifest GenerateManifest(
        AgentTaskGraphEntry task,
        IReadOnlyList<TaskEvidenceLink> links)
    {
        var taskLinks = links.Where(l => l.TaskId == task.TaskId).ToList();
        
        var totalLinks = taskLinks.Count;
        var filesChanged = taskLinks.SelectMany(l => l.ChangedFilePaths).Distinct().Count();
        var commandsExecuted = taskLinks.SelectMany(l => l.ExecutedCommands).Distinct().Count();
        var gatesReferenced = taskLinks.SelectMany(l => l.QualityGateReferences).Distinct().Count();

        // Recovery tasks may not change files, but should have commands and gate references
        var hasCompleteEvidence = task.TaskId.StartsWith("t_recovery_")
            ? commandsExecuted >= MinCommandsExecutedForEvidence && gatesReferenced > 0
            : filesChanged >= MinFilesChangedForEvidence && commandsExecuted >= MinCommandsExecutedForEvidence && gatesReferenced > 0;

        var manifest = new TaskEvidenceManifest(
            task.TaskId,
            task.Notes ?? "unknown",
            totalLinks,
            filesChanged,
            commandsExecuted,
            gatesReferenced,
            hasCompleteEvidence);

        _logger.LogInformation(
            "Generated manifest for task {TaskId}: {Files} files, {Commands} commands, {Gates} gates, complete={Complete}",
            task.TaskId, filesChanged, commandsExecuted, gatesReferenced, hasCompleteEvidence);

        return manifest;
    }

    public bool ValidateEvidenceLinkage(
        AgentTaskGraphEntry task,
        IReadOnlyList<TaskEvidenceLink> links)
    {
        var taskLinks = links.Where(l => l.TaskId == task.TaskId).ToList();

        // Tasks that are not executed (Pending, Ready, Blocked) don't need evidence
        if (task.State is not (AgentTaskState.Done or AgentTaskState.Failed))
        {
            _logger.LogDebug(
                "Task {TaskId} is in state {State}, skipping evidence validation",
                task.TaskId, task.State);
            return true;
        }

        // Failed tasks should have evidence of what failed
        if (task.State == AgentTaskState.Failed)
        {
            var hasAnyEvidence = taskLinks.Any(l =>
                l.ExecutedCommands.Count > 0 || l.QualityGateReferences.Count > 0);

            if (!hasAnyEvidence)
            {
                _logger.LogWarning(
                    "Failed task {TaskId} has no evidence of execution",
                    task.TaskId);
                return false;
            }

            return true;
        }

        // Done tasks should have complete evidence
        var manifest = GenerateManifest(task, links);
        if (!manifest.HasCompleteEvidence)
        {
            _logger.LogWarning(
                "Task {TaskId} has incomplete evidence: files={Files}, commands={Commands}, gates={Gates}",
                task.TaskId, manifest.FilesChanged, manifest.CommandsExecuted, manifest.GatesReferenced);
            return false;
        }

        return true;
    }

    public IReadOnlyList<TaskEvidenceLink> GetLinksForGraph(
        IReadOnlyList<AgentTaskGraphEntry> graph,
        IReadOnlyList<TaskEvidenceLink> allLinks)
    {
        var taskIds = graph.Select(t => t.TaskId).ToHashSet();
        var result = allLinks.Where(l => taskIds.Contains(l.TaskId)).ToList();

        _logger.LogInformation(
            "Retrieved {Count} evidence links for task graph with {Tasks} tasks",
            result.Count, graph.Count);

        return result;
    }
}
