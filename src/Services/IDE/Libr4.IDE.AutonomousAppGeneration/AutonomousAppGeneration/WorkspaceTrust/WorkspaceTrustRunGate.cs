using System.Collections.Concurrent;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;

public sealed class WorkspaceTrustRunGate : IWorkspaceTrustRunGate
{
    private sealed class RunEntry
    {
        public required Guid RunId { get; init; }
        public required string WorkspaceHash { get; init; }
        public WorkspaceTrustPrompt? PendingPrompt;
        public WorkspaceTrustDecision? Decision;
        public TaskCompletionSource<bool>? Completion;
    }

    private readonly IWorkspaceTrustService _trust;
    private readonly IAgentRunPermissionStore _permissions;
    private readonly WorkspaceTrustOptions _options;
    private readonly ILogger<WorkspaceTrustRunGate> _logger;
    private readonly ConcurrentDictionary<Guid, RunEntry> _runs = new();

    public WorkspaceTrustRunGate(
        IWorkspaceTrustService trust,
        IAgentRunPermissionStore permissions,
        IOptions<WorkspaceTrustOptions> options,
        ILogger<WorkspaceTrustRunGate> logger)
    {
        _trust = trust;
        _permissions = permissions;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<WorkspaceTrustRunState> BeginRunAsync(Guid runId, string workspaceHash, CancellationToken ct = default)
    {
        var resolution = await _trust.ResolveAsync(workspaceHash, ct).ConfigureAwait(false);
        var entry = new RunEntry
        {
            RunId = runId,
            WorkspaceHash = workspaceHash
        };

        if (resolution.NeedsFirstRunPrompt && resolution.Prompt is not null)
        {
            entry.PendingPrompt = resolution.Prompt;
            entry.Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _runs[runId] = entry;
            return ToState(entry, awaitingPrompt: true);
        }

        entry.Decision = resolution.Decision!;
        ApplyDecision(runId, entry.Decision);
        _runs[runId] = entry;
        return ToState(entry, awaitingPrompt: false);
    }

    public async Task WaitForDecisionAsync(Guid runId, CancellationToken ct = default)
    {
        if (!_runs.TryGetValue(runId, out var entry) || entry.Completion is null)
            return;

        await entry.Completion.Task.WaitAsync(ct).ConfigureAwait(false);
    }

    public WorkspaceTrustRunState? GetRunState(Guid runId) =>
        _runs.TryGetValue(runId, out var entry) ? ToState(entry, entry.PendingPrompt is not null) : null;

    public bool DenyCloudInference(Guid runId) =>
        _runs.TryGetValue(runId, out var entry)
        && entry.Decision is not null
        && entry.Decision.DenyCloudInference;

    public async Task ResolveAsync(Guid runId, WorkspaceTrustResolveRequest request, CancellationToken ct = default)
    {
        if (!_runs.TryGetValue(runId, out var entry))
            throw new InvalidOperationException($"Run {runId} has no workspace trust state");

        if (entry.PendingPrompt is null
            || !string.Equals(entry.PendingPrompt.PromptId, request.PromptId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Workspace trust prompt mismatch or already resolved");

        var decision = new WorkspaceTrustDecision(
            entry.WorkspaceHash,
            request.SandboxPolicy,
            request.HostMode,
            FromStore: false,
            FromConfigOverride: false,
            WorkspaceTrustPolicyMapper.DeniesCloudInference(request.HostMode));

        if (request.RememberChoice && !_options.BypassTrustStore)
        {
            await _trust.RememberAsync(
                    new WorkspaceTrustRecord(
                        entry.WorkspaceHash,
                        request.SandboxPolicy,
                        request.HostMode,
                        DateTime.UtcNow),
                    ct)
                .ConfigureAwait(false);
            decision = decision with { FromStore = true };
        }

        entry.PendingPrompt = null;
        entry.Decision = decision;
        ApplyDecision(runId, decision);
        entry.Completion?.TrySetResult(true);

        _logger.LogInformation(
            "Workspace trust resolved for run {RunId}: sandbox={Sandbox}, host={Host}, remembered={Remember}",
            runId,
            request.SandboxPolicy,
            request.HostMode,
            request.RememberChoice);
    }

    private void ApplyDecision(Guid runId, WorkspaceTrustDecision decision)
    {
        _permissions.Set(runId, WorkspaceTrustPolicyMapper.ToPermissionMode(decision.SandboxPolicy));
    }

    private static WorkspaceTrustRunState ToState(RunEntry entry, bool awaitingPrompt) =>
        new(
            entry.RunId,
            entry.WorkspaceHash,
            IsReady: entry.Decision is not null,
            AwaitingPrompt: awaitingPrompt,
            entry.PendingPrompt,
            entry.Decision);
}
