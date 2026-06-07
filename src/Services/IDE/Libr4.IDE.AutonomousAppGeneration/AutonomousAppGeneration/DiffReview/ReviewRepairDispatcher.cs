using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public interface IReviewRepairDispatcher
{
    void DispatchScopedRepair(Guid runId, IReadOnlyList<string> paths, string? notes);
}

public sealed class ReviewRepairDispatcher : IReviewRepairDispatcher
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ReviewRepairDispatcher> _logger;

    public ReviewRepairDispatcher(
        IServiceScopeFactory scopes,
        ILogger<ReviewRepairDispatcher> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public void DispatchScopedRepair(Guid runId, IReadOnlyList<string> paths, string? notes)
    {
        if (paths.Count == 0)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await RunScopedRepairAsync(runId, paths, notes, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ReviewRepair] Background repair failed for run {RunId}", runId);
            }
        });
    }

    private async Task RunScopedRepairAsync(
        Guid runId,
        IReadOnlyList<string> paths,
        string? notes,
        CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var repository = scope.ServiceProvider.GetService<IAppGenerationRepository>();
        var specs = scope.ServiceProvider.GetService<IAgentSpecRegistry>();
        var runner = scope.ServiceProvider.GetService<IAgentSpecSubagentRunner>();
        var accessor = scope.ServiceProvider.GetService<IShadowWorkspaceAccessor>();
        var subagents = scope.ServiceProvider.GetService<ISubagentStore>();

        if (repository is null || specs is null || runner is null || accessor is null || subagents is null)
        {
            _logger.LogWarning("[ReviewRepair] Missing services for run {RunId}", runId);
            return;
        }

        var orchestrator = await repository.GetAsync(runId, ct).ConfigureAwait(false);
        if (orchestrator?.Plan is null || orchestrator.ShadowWorkspaceId is not Guid workspaceId)
        {
            _logger.LogWarning("[ReviewRepair] Orchestrator/plan/workspace missing for run {RunId}", runId);
            return;
        }

        if (!specs.TryGet("repair", out var repairSpec))
        {
            _logger.LogWarning("[ReviewRepair] repair agent spec not found");
            return;
        }

        var scopedFiles = ReviewRepairScopeHelper.SelectScopedFiles(orchestrator.Files, paths);

        if (scopedFiles.Count == 0)
        {
            _logger.LogWarning("[ReviewRepair] No matching files for run {RunId}", runId);
            return;
        }

        if (!accessor.TryGetWorkspace(workspaceId, out var workspace))
        {
            _logger.LogWarning("[ReviewRepair] Workspace {WorkspaceId} unavailable for run {RunId}", workspaceId, runId);
            return;
        }

        var task = ReviewRepairScopeHelper.BuildRepairTask(paths, notes);
        var record = await subagents.CreateAsync(runId, "repair", task, repairSpec, ct).ConfigureAwait(false);
        await subagents.AppendMessageAsync(runId, record.Id, "user", task, ct).ConfigureAwait(false);

        var toolContext = new ToolContext
        {
            Workspace = workspace,
            Accessor = accessor,
            WorkingFiles = scopedFiles.ToList(),
            FileState = new FileStateCache(),
            Plan = orchestrator.Plan,
            BuildLog = BuildReviewRepairLog(paths, notes),
            Mode = AgentSessionMode.Repair,
            Session = new AgentSessionState { RunId = runId }
        };

        var result = await runner.RunAsync(repairSpec, task, toolContext, ct).ConfigureAwait(false);
        var output = result.Summary ?? (result.Succeeded ? "repair complete" : "repair failed");

        await subagents.AppendMessageAsync(runId, record.Id, "assistant", output, ct).ConfigureAwait(false);
        if (result.Succeeded)
            await subagents.CompleteAsync(runId, record.Id, output, ct).ConfigureAwait(false);
        else
            await subagents.FailAsync(runId, record.Id, output, ct).ConfigureAwait(false);
    }

    private static string BuildReviewRepairLog(IReadOnlyList<string> paths, string? notes) =>
        $"human_review_repair paths=[{string.Join(", ", paths)}] notes={notes ?? "(none)"}";
}
