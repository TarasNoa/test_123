using System.Text;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;

public interface ICiRepairDispatcher
{
    void DispatchCiFailureRepair(Guid runId, string? ciLogsUrl);
}

public sealed class CiRepairDispatcher : ICiRepairDispatcher
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IGitHubCiLogPrefetcher _logPrefetcher;
    private readonly CiRepairOptions _options;
    private readonly ILogger<CiRepairDispatcher> _logger;

    public CiRepairDispatcher(
        IServiceScopeFactory scopes,
        IGitHubCiLogPrefetcher logPrefetcher,
        IOptions<CiRepairOptions> options,
        ILogger<CiRepairDispatcher> logger)
    {
        _scopes = scopes;
        _logPrefetcher = logPrefetcher;
        _options = options.Value;
        _logger = logger;
    }

    public void DispatchCiFailureRepair(Guid runId, string? ciLogsUrl)
    {
        if (!_options.AutoSpawnRepairOnCiFail)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await RunCiRepairAsync(runId, ciLogsUrl, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CiRepair] Background repair failed for run {RunId}", runId);
            }
        });
    }

    private async Task RunCiRepairAsync(Guid runId, string? ciLogsUrl, CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var repository = scope.ServiceProvider.GetService<IAppGenerationRepository>();
        var specs = scope.ServiceProvider.GetService<IAgentSpecRegistry>();
        var runner = scope.ServiceProvider.GetService<IAgentSpecSubagentRunner>();
        var accessor = scope.ServiceProvider.GetService<IShadowWorkspaceAccessor>();
        var subagents = scope.ServiceProvider.GetService<ISubagentStore>();
        var fastContext = scope.ServiceProvider.GetService<IFastContextPrefetcher>();

        if (repository is null || specs is null || runner is null || accessor is null || subagents is null)
        {
            _logger.LogWarning("[CiRepair] Missing services for run {RunId}", runId);
            return;
        }

        var orchestrator = await repository.GetAsync(runId, ct).ConfigureAwait(false);
        if (orchestrator?.Plan is null || orchestrator.ShadowWorkspaceId is not Guid workspaceId)
        {
            _logger.LogWarning("[CiRepair] Orchestrator/plan/workspace missing for run {RunId}", runId);
            return;
        }

        if (!specs.TryGet("repair", out var repairSpec))
        {
            _logger.LogWarning("[CiRepair] repair agent spec not found");
            return;
        }

        if (!accessor.TryGetWorkspace(workspaceId, out var workspace))
        {
            _logger.LogWarning("[CiRepair] Workspace {WorkspaceId} unavailable for run {RunId}", workspaceId, runId);
            return;
        }

        var rawLogs = await _logPrefetcher.PrefetchAsync(ciLogsUrl, ct).ConfigureAwait(false);
        var parsed = CiRepairLogParser.Parse(rawLogs, _options.MaxLogChars, _options.MaxExcerptLines);
        var focusPaths = parsed.FocusPaths;
        var scopedFiles = focusPaths.Count > 0
            ? ReviewRepairScopeHelper.SelectScopedFiles(orchestrator.Files, focusPaths)
            : orchestrator.Files.ToList();

        if (scopedFiles.Count == 0)
            scopedFiles = orchestrator.Files.ToList();

        var buildLog = BuildCiRepairLog(ciLogsUrl, parsed.Excerpt);
        string? prefetchText = null;
        if (fastContext is not null)
        {
            var prefetch = await fastContext.PrefetchForRepairAsync(
                new FastContextPrefetchRequest(
                    workspace.HostPath,
                    buildLog,
                    parsed.Errors,
                    orchestrator.Files,
                    orchestrator.UserRequest,
                    runId),
                ct).ConfigureAwait(false);
            prefetchText = prefetch.FormattedText;
        }

        var task = CiRepairLogParser.BuildRepairTask(focusPaths, parsed.Excerpt, prefetchText);
        var record = await subagents.CreateAsync(runId, "repair", task, repairSpec, ct).ConfigureAwait(false);
        await subagents.AppendMessageAsync(runId, record.Id, "user", task, ct).ConfigureAwait(false);

        var toolContext = new ToolContext
        {
            Workspace = workspace,
            Accessor = accessor,
            WorkingFiles = scopedFiles.ToList(),
            FileState = new FileStateCache(),
            Plan = orchestrator.Plan,
            BuildLog = buildLog,
            Mode = AgentSessionMode.Repair,
            Session = new AgentSessionState { RunId = runId }
        };

        var result = await runner.RunAsync(repairSpec, task, toolContext, ct).ConfigureAwait(false);
        var output = result.Summary ?? (result.Succeeded ? "ci repair complete" : "ci repair failed");

        await subagents.AppendMessageAsync(runId, record.Id, "assistant", output, ct).ConfigureAwait(false);
        if (result.Succeeded)
            await subagents.CompleteAsync(runId, record.Id, output, ct).ConfigureAwait(false);
        else
            await subagents.FailAsync(runId, record.Id, output, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[CiRepair] Completed CI repair subagent for run {RunId} success={Success} scopedFiles={Count}",
            runId,
            result.Succeeded,
            scopedFiles.Count);
    }

    private static string BuildCiRepairLog(string? ciLogsUrl, string excerpt) =>
        $"ci_failure_repair url={ciLogsUrl ?? "(none)"}\n{excerpt}";
}
