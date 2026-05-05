using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class AgentIntegrationCoordinator : IAgentIntegrationCoordinator
{
    private const int MemoryTokenBudget = 24_000;

    private readonly IAgentTaskGraphService _taskGraph;
    private readonly IMemoryStore _memory;
    private readonly IAutonomousCascadePlanner _cascadePlanner;
    private readonly ISkillRunner _skillRunner;
    private readonly IContextPackBuilder _context;
    private readonly ISecurityReviewGateService _security;
    private readonly IAdaptiveReplannerService? _adaptiveReplanner;
    private readonly ITaskEvidenceLinkageService? _taskEvidence;
    private readonly ILogger<AgentIntegrationCoordinator> _logger;
    private readonly ConcurrentDictionary<Guid, List<TaskEvidenceLink>> _taskEvidenceByRun = new();

    public AgentIntegrationCoordinator(
        IAgentTaskGraphService taskGraph,
        IMemoryStore memory,
        IAutonomousCascadePlanner cascadePlanner,
        ISkillRunner skillRunner,
        IContextPackBuilder context,
        ISecurityReviewGateService security,
        IAdaptiveReplannerService? adaptiveReplanner,
        ITaskEvidenceLinkageService? taskEvidence,
        ILogger<AgentIntegrationCoordinator> logger)
    {
        _taskGraph = taskGraph;
        _memory = memory;
        _cascadePlanner = cascadePlanner;
        _skillRunner = skillRunner;
        _context = context;
        _security = security;
        _adaptiveReplanner = adaptiveReplanner;
        _taskEvidence = taskEvidence;
        _logger = logger;
    }

    public AgentIntegrationCoordinator(
        IAgentTaskGraphService taskGraph,
        IMemoryStore memory,
        IAutonomousCascadePlanner cascadePlanner,
        ISkillRunner skillRunner,
        IContextPackBuilder context,
        ISecurityReviewGateService security,
        ILogger<AgentIntegrationCoordinator> logger)
        : this(taskGraph, memory, cascadePlanner, skillRunner, context, security, null, null, logger)
    {
    }

    public async Task OnPlanAttachedAsync(AppGenerationOrchestrator orchestrator, GenerationPlan plan, CancellationToken ct)
    {
        var cascade = _cascadePlanner.Build(plan, orchestrator.UserRequest);
        orchestrator.RecordCascadePlan(new CascadePlanAuditEntry(
            RunId: orchestrator.Id,
            Rationale: cascade.Rationale,
            SerializedPlanJson: cascade.OrchestratorJson,
            PhaseCount: cascade.Phases.Count,
            RoutingProfile: cascade.RoutingProfile,
            ModelHint: cascade.ModelHint,
            PlannerMode: cascade.PlannerMode,
            CreatedAtUtc: DateTime.UtcNow));

        var graph = _taskGraph.BuildInitial(plan, orchestrator.UserRequest);
        var marked = _taskGraph.Transition(graph, "t_plan", AgentTaskState.Done);
        orchestrator.ReplaceTaskGraph(marked);
        await IngestPlanAndSkillsAsync(orchestrator, plan, ct).ConfigureAwait(false);
    }

    public Task IngestGenerationArtifactsAsync(
        AppGenerationOrchestrator orchestrator,
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> files,
        CancellationToken ct) =>
        IngestGenerationMemoryAsync(orchestrator, plan, files, ct);

    public async Task OnGenerationGatePassedAsync(
        AppGenerationOrchestrator orchestrator,
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> files,
        CancellationToken ct)
    {
        if (orchestrator.TaskGraph.Count == 0)
            return;

        var g = _taskGraph.Transition(
            orchestrator.TaskGraph,
            "t_generate",
            AgentTaskState.Done,
            BuildEvidencePaths(orchestrator, "t_generate", files.Select(f => f.RelativePath).Take(64).ToList(), "generation"));
        orchestrator.ReplaceTaskGraph(g);

        await _skillRunner.RecordStageSelectionAsync(orchestrator, "generation", plan, ct).ConfigureAwait(false);
        await IngestContextPackAsync(orchestrator, "post_generation", ct).ConfigureAwait(false);
        await _memory.PruneAsync(orchestrator.RequestFingerprint, MemoryTokenBudget, ct).ConfigureAwait(false);
    }

    public async Task OnPostConsistencyAsync(AppGenerationOrchestrator orchestrator, GenerationPlan plan, CancellationToken ct)
    {
        var g = orchestrator.TaskGraph.Count > 0
            ? _taskGraph.Transition(
                orchestrator.TaskGraph,
                "t_consistency",
                AgentTaskState.Done,
                BuildEvidencePaths(orchestrator, "t_consistency", Array.Empty<string>(), "consistency"))
            : orchestrator.TaskGraph;
        orchestrator.ReplaceTaskGraph(g);

        await _skillRunner.RecordStageSelectionAsync(orchestrator, "consistency", plan, ct).ConfigureAwait(false);
        await IngestContextPackAsync(orchestrator, "post_consistency", ct).ConfigureAwait(false);
    }

    public Task OnWorkspaceAttachedAsync(AppGenerationOrchestrator orchestrator, Guid workspaceId, CancellationToken ct)
    {
        _ = ct;
        if (orchestrator.TaskGraph.Count == 0)
            return Task.CompletedTask;

        var g = _taskGraph.Transition(
            orchestrator.TaskGraph,
            "t_workspace",
            AgentTaskState.Done,
            BuildEvidencePaths(orchestrator, "t_workspace", new[] { workspaceId.ToString("D") }, "workspace"));
        orchestrator.ReplaceTaskGraph(g);
        return Task.CompletedTask;
    }

    public async Task OnPhaseBuildSucceededAsync(
        AppGenerationOrchestrator orchestrator,
        GenerationPlan plan,
        string phaseName,
        CancellationToken ct)
    {
        var taskId = orchestrator.TaskGraph
            .FirstOrDefault(t => t.Title.Contains(phaseName, StringComparison.OrdinalIgnoreCase))?.TaskId;
        IReadOnlyList<AgentTaskGraphEntry> g = orchestrator.TaskGraph;
        if (taskId is not null)
            g = _taskGraph.Transition(
                g,
                taskId,
                AgentTaskState.Done,
                BuildEvidencePaths(orchestrator, taskId, new[] { phaseName }, $"build:{phaseName}"));

        orchestrator.ReplaceTaskGraph(g);
        await _skillRunner.RecordStageSelectionAsync(orchestrator, $"build:{phaseName}", plan, ct)
            .ConfigureAwait(false);
    }

    public async Task OnGateFailureAsync(
        AppGenerationOrchestrator orchestrator,
        string stage,
        IReadOnlyList<string> reasons,
        CancellationToken ct)
    {
        if (orchestrator.TaskGraph.Count == 0)
        {
            await IngestFailureMemoryAsync(orchestrator, stage, reasons, ct).ConfigureAwait(false);
            return;
        }

        var g = _taskGraph.AppendRecoveryTasks(orchestrator.TaskGraph, stage, reasons);
        g = AppendAdaptiveRecoveryTasks(orchestrator, g, stage);
        orchestrator.ReplaceTaskGraph(g);
        await IngestFailureMemoryAsync(orchestrator, stage, reasons, ct).ConfigureAwait(false);
    }

    public async Task OnPostFixAsync(AppGenerationOrchestrator orchestrator, GenerationPlan plan, CancellationToken ct)
    {
        await _skillRunner.RecordStageSelectionAsync(orchestrator, "fixing", plan, ct).ConfigureAwait(false);
        await IngestContextPackAsync(orchestrator, "post_fix", ct).ConfigureAwait(false);
    }

    public SecurityReviewAuditEntry ReviewGeneratedCode(
        string stage,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan) =>
        _security.EvaluateArtifacts(stage, files, plan);

    private async Task IngestPlanAndSkillsAsync(AppGenerationOrchestrator orchestrator, GenerationPlan plan, CancellationToken ct)
    {
        var summary =
            $"{plan.ApplicationName}: phases={plan.Phases.Count}; agents={plan.RequiredAgents.Count}; " +
            $"build={plan.BuildCommands.Count}; tests={plan.TestCommands.Count}";
        var key = $"plan:{plan.ApplicationName}";
        var tokens = EstimateTokens(summary);
        var record = new MemoryRecord(
            orchestrator.Id,
            orchestrator.RequestFingerprint,
            "plan",
            MemoryKind.Semantic,
            key,
            summary,
            null,
            tokens,
            DateTime.UtcNow);
        await _memory.IngestAsync(record, ct).ConfigureAwait(false);
        orchestrator.RecordMemoryIngest(new MemoryIngestAuditEntry(
            orchestrator.Id,
            "plan",
            MemoryKind.Semantic,
            key,
            summary,
            tokens,
            DateTime.UtcNow));

        await _skillRunner.RecordStageSelectionAsync(orchestrator, "planning", plan, ct).ConfigureAwait(false);
        await IngestContextPackAsync(orchestrator, "post_plan", ct).ConfigureAwait(false);
        _logger.LogDebug("Agent integration: plan memory + task graph initialized for run {RunId}", orchestrator.Id);
    }

    private async Task IngestGenerationMemoryAsync(
        AppGenerationOrchestrator orchestrator,
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> files,
        CancellationToken ct)
    {
        _ = plan;
        var summary = $"generated_files={files.Count}; languages={string.Join(',', files.Select(f => f.Language).Distinct())}";
        var key = $"generation:{orchestrator.Id:N}";
        var tokens = EstimateTokens(summary);
        var record = new MemoryRecord(
            orchestrator.Id,
            orchestrator.RequestFingerprint,
            "generation",
            MemoryKind.Episodic,
            key,
            summary,
            null,
            tokens,
            DateTime.UtcNow);
        await _memory.IngestAsync(record, ct).ConfigureAwait(false);
        orchestrator.RecordMemoryIngest(new MemoryIngestAuditEntry(
            orchestrator.Id,
            "generation",
            MemoryKind.Episodic,
            key,
            summary,
            tokens,
            DateTime.UtcNow));
    }

    private async Task IngestFailureMemoryAsync(
        AppGenerationOrchestrator orchestrator,
        string stage,
        IReadOnlyList<string> reasons,
        CancellationToken ct)
    {
        var summary = $"{stage}: {string.Join(" | ", reasons.Take(6))}";
        var key = $"failure:{stage}:{orchestrator.Id:N}";
        var tokens = EstimateTokens(summary);
        var record = new MemoryRecord(
            orchestrator.Id,
            orchestrator.RequestFingerprint,
            stage,
            MemoryKind.Episodic,
            key,
            summary,
            null,
            tokens,
            DateTime.UtcNow);
        await _memory.IngestAsync(record, ct).ConfigureAwait(false);
        orchestrator.RecordMemoryIngest(new MemoryIngestAuditEntry(
            orchestrator.Id,
            stage,
            MemoryKind.Episodic,
            key,
            summary,
            tokens,
            DateTime.UtcNow));
    }

    private async Task IngestContextPackAsync(AppGenerationOrchestrator orchestrator, string stage, CancellationToken ct)
    {
        var pack = await _context.BuildPackAsync(stage, orchestrator, 32_000, ct).ConfigureAwait(false);
        var key = $"context:{stage}";
        var tokens = EstimateTokens(pack);
        var record = new MemoryRecord(
            orchestrator.Id,
            orchestrator.RequestFingerprint,
            stage,
            MemoryKind.Semantic,
            key,
            $"context_pack:{stage}",
            pack,
            tokens,
            DateTime.UtcNow);
        await _memory.IngestAsync(record, ct).ConfigureAwait(false);
        orchestrator.RecordMemoryIngest(new MemoryIngestAuditEntry(
            orchestrator.Id,
            stage,
            MemoryKind.Semantic,
            key,
            record.Summary,
            tokens,
            DateTime.UtcNow));

        // Record memory retrievals with explainable provenance
        var retrieved = await _memory.RetrieveAsync(
            new MemoryQuery(
                orchestrator.RequestFingerprint,
                Keyword: null,
                TopK: 10),
            ct).ConfigureAwait(false);
        foreach (var r in retrieved)
        {
            orchestrator.RecordMemoryRetrieval(new MemoryRetrievalAuditEntry(
                orchestrator.Id,
                stage,
                r.Record.Kind,
                r.Record.Key,
                r.Record.Summary,
                r.RetrievalReason,
                r.RelevanceScore,
                DateTime.UtcNow));
        }
    }

    private static int EstimateTokens(string text) => Math.Max(1, text.Length / 4);

    private IReadOnlyList<AgentTaskGraphEntry> AppendAdaptiveRecoveryTasks(
        AppGenerationOrchestrator orchestrator,
        IReadOnlyList<AgentTaskGraphEntry> currentGraph,
        string stage)
    {
        if (_adaptiveReplanner is null)
            return currentGraph;

        var signatures = _adaptiveReplanner.DetectFailureSignatures(
            orchestrator.QualityGates.Select(g => new QualityGateResult(g.Stage, g.Score, g.Passed, g.Reasons ?? Array.Empty<string>()))
                .ToList());
        var stageSignatures = signatures
            .Where(s => s.Stage.Equals(stage, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (stageSignatures.Count == 0)
            return currentGraph;

        var recommendations = _adaptiveReplanner.GenerateRecoveryTasks(stageSignatures, currentGraph);
        if (recommendations.Count == 0)
            return currentGraph;

        var updated = currentGraph.ToList();
        foreach (var task in recommendations)
        {
            updated.Add(new AgentTaskGraphEntry(
                task.TaskId,
                task.Description,
                Array.Empty<string>(),
                AgentTaskState.Ready,
                BuildEvidencePaths(orchestrator, task.TaskId, Array.Empty<string>(), $"adaptive_recovery:{task.Stage}"),
                task.Rationale));
        }

        _logger.LogInformation(
            "Adaptive re-planner appended {Count} recovery task(s) for stage {Stage}",
            recommendations.Count, stage);

        return updated;
    }

    private IReadOnlyList<string> BuildEvidencePaths(
        AppGenerationOrchestrator orchestrator,
        string taskId,
        IReadOnlyList<string> changedFiles,
        string stage)
    {
        if (_taskEvidence is null)
            return changedFiles;

        var task = orchestrator.TaskGraph.FirstOrDefault(t => t.TaskId == taskId)
            ?? new AgentTaskGraphEntry(taskId, taskId, Array.Empty<string>(), AgentTaskState.Done, Array.Empty<string>(), stage);

        var commands = orchestrator.Iterations
            .SelectMany(i => i.Execution?.CommandExecutions ?? Array.Empty<CommandExecutionRecord>())
            .Select(c => c.Command)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
        var gateRefs = orchestrator.QualityGates
            .Select(g => g.Stage)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();

        var evidence = _taskEvidence.LinkTaskToEvidence(task, changedFiles, commands, gateRefs);
        var perRun = _taskEvidenceByRun.GetOrAdd(orchestrator.Id, _ => new List<TaskEvidenceLink>());
        lock (perRun)
        {
            perRun.Add(evidence);
        }

        // Compact trace references that are visible in task graph evidence paths.
        var refs = new List<string>(changedFiles);
        refs.Add($"evidence:commands={evidence.ExecutedCommands.Count}");
        refs.Add($"evidence:gates={evidence.QualityGateReferences.Count}");
        return refs.Distinct(StringComparer.OrdinalIgnoreCase).Take(64).ToList();
    }
}
