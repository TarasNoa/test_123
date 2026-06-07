using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public sealed class SpaceOrchestrator : ISpaceOrchestrator
{
    private static readonly string[] ContextReadyKinds =
    [
        "space_context_ready",
        "explorer_complete",
        "plan",
        "plan_summary"
    ];

    private readonly IAgentSpaceService _spaces;
    private readonly ISpaceContextBus _context;
    private readonly ISpaceConcurrencyGate _gate;
    private readonly AgentSpaceOptions _options;
    private readonly ILogger<SpaceOrchestrator> _logger;

    public SpaceOrchestrator(
        IAgentSpaceService spaces,
        ISpaceContextBus context,
        ISpaceConcurrencyGate gate,
        IOptions<AgentSpaceOptions> options,
        ILogger<SpaceOrchestrator> logger)
    {
        _spaces = spaces;
        _context = context;
        _gate = gate;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SpaceOrchestrationResult> RunParallelPipelineAsync(
        Guid spaceId,
        SpaceOrchestrationRequest request,
        CancellationToken ct = default)
    {
        var detail = await _spaces.GetSpaceDetailAsync(spaceId, ct).ConfigureAwait(false)
                     ?? throw new KeyNotFoundException("space_not_found");

        var timeout = TimeSpan.FromSeconds(
            request.ContextReadyTimeoutSeconds > 0
                ? request.ContextReadyTimeoutSeconds
                : _options.OrchestratorContextReadySeconds);

        SpaceMember explorer;
        using (await _gate.AcquireLlmSlotAsync(spaceId, ct).ConfigureAwait(false))
        {
            explorer = await _spaces.SpawnAgentAsync(
                spaceId,
                new SpawnSpaceAgentRequest(SpaceMemberRole.Explorer, request.ExplorerTask, request.ExplorerRunId),
                ct).ConfigureAwait(false);
        }

        await _context.PublishAsync(spaceId, "orchestrator_phase", "Explorer started", request.ExplorerTask, explorer.MemberId, ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(request.ExplorerTask))
        {
            await _context.PublishAsync(spaceId, "explorer_complete", "Explorer finished", request.ExplorerTask, explorer.MemberId, ct).ConfigureAwait(false);
            await _context.PublishAsync(spaceId, "space_context_ready", "Space context ready", request.ExplorerTask, explorer.MemberId, ct).ConfigureAwait(false);
        }
        else
        {
            var ready = await WaitForContextKindsAsync(spaceId, ContextReadyKinds, timeout, ct).ConfigureAwait(false);
            if (!ready)
            {
                await _context.PublishAsync(
                    spaceId,
                    "space_context_ready",
                    "Explorer timeout fallback",
                    "continue with partial context",
                    explorer.MemberId,
                    ct).ConfigureAwait(false);
            }
        }

        SpaceMember implementer;
        using (await _gate.AcquireLlmSlotAsync(spaceId, ct).ConfigureAwait(false))
        {
            implementer = await _spaces.SpawnAgentAsync(
                spaceId,
                new SpawnSpaceAgentRequest(SpaceMemberRole.Implementer, request.ImplementerTask, request.ImplementerRunId),
                ct).ConfigureAwait(false);
        }

        await _context.PublishAsync(
            spaceId,
            "implementer_checkpoint",
            "Implementer checkpoint",
            request.ImplementerTask,
            implementer.MemberId,
            ct).ConfigureAwait(false);

        var merge = await _spaces.MergeMemberAsync(spaceId, implementer.MemberId, ct).ConfigureAwait(false);
        if (!merge.Success)
        {
            _logger.LogWarning(
                "Implementer merge conflict in space {SpaceId}: {Conflicts}",
                spaceId,
                string.Join(", ", merge.Conflicts));
        }

        SpaceMember? verifier = null;
        if (!request.SkipVerifier)
        {
            using (await _gate.AcquireLlmSlotAsync(spaceId, ct).ConfigureAwait(false))
            {
                verifier = await _spaces.SpawnAgentAsync(
                    spaceId,
                    new SpawnSpaceAgentRequest(
                        SpaceMemberRole.Verifier,
                        request.VerifierTask,
                        request.VerifierRunId,
                        BindToIntegrationWorktree: true),
                    ct).ConfigureAwait(false);
            }

            await _context.PublishAsync(
                spaceId,
                "verifier_started",
                "Verifier on integration branch",
                detail.Space.IntegrationBranch,
                verifier.MemberId,
                ct).ConfigureAwait(false);
        }

        var timeline = await _context.ReadRecentAsync(spaceId, 64, ct).ConfigureAwait(false);
        return new SpaceOrchestrationResult(
            spaceId,
            explorer,
            implementer,
            verifier,
            ContextReady: true,
            Stage: merge.Success
                ? (request.SkipVerifier ? "implementer_merged" : "verifier_spawned")
                : "merge_conflict",
            Timeline: timeline);
    }

    public Task SignalContextReadyAsync(
        Guid spaceId,
        string memberId,
        string kind,
        string title,
        string? payload,
        CancellationToken ct = default) =>
        _context.PublishAsync(spaceId, kind, title, payload, memberId, ct);

    private async Task<bool> WaitForContextKindsAsync(
        Guid spaceId,
        IReadOnlyList<string> kinds,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var events = await _context.ReadRecentAsync(spaceId, 48, ct).ConfigureAwait(false);
            if (events.Any(e => kinds.Contains(e.Kind, StringComparer.OrdinalIgnoreCase)))
                return true;

            await Task.Delay(TimeSpan.FromMilliseconds(250), ct).ConfigureAwait(false);
        }

        return false;
    }
}
