using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public sealed class AgentFleetRegistry : IAgentFleetRegistry
{
    private readonly IAgentFleetIndexStore _index;
    private readonly IAppGenerationRepository _repository;
    private readonly IAutonomousRunControlService _runControl;
    private readonly IFlowProgressStore _flowProgress;
    private readonly AgentFleetOptions _options;
    private readonly ILogger<AgentFleetRegistry> _logger;
    private readonly IAgentFleetEventHub? _eventHub;
    private readonly IRunUsageRollupService? _usageRollup;
    private readonly IBudgetService? _budget;
    private readonly IFleetShipStateStore? _shipState;
    private readonly IFleetShipSyncService? _shipSync;
    private readonly IFleetSessionSearchService? _sessionSearch;
    private readonly IFleetSimilarRunsService? _similarRuns;
    private readonly Dictionary<Guid, DateTime> _verifyStartedAt = new();

    public event Func<AgentFleetStatusEvent, Task>? StatusChanged;

    public AgentFleetRegistry(
        IAgentFleetIndexStore index,
        IAppGenerationRepository repository,
        IAutonomousRunControlService runControl,
        IFlowProgressStore flowProgress,
        IOptions<AgentFleetOptions> options,
        ILogger<AgentFleetRegistry> logger,
        IAgentFleetEventHub? eventHub = null,
        IRunUsageRollupService? usageRollup = null,
        IBudgetService? budget = null,
        IFleetShipStateStore? shipState = null,
        IFleetShipSyncService? shipSync = null,
        IFleetSessionSearchService? sessionSearch = null,
        IFleetSimilarRunsService? similarRuns = null)
    {
        _index = index;
        _repository = repository;
        _runControl = runControl;
        _flowProgress = flowProgress;
        _options = options.Value;
        _logger = logger;
        _eventHub = eventHub;
        _usageRollup = usageRollup;
        _budget = budget;
        _shipState = shipState;
        _shipSync = shipSync;
        _sessionSearch = sessionSearch;
        _similarRuns = similarRuns;
    }

    public Task EnsureSchemaAsync(CancellationToken ct = default) =>
        _index.EnsureSchemaAsync(ct);

    public async Task<IReadOnlyList<AgentFleetSummary>> ListAsync(AgentFleetListQuery query, CancellationToken ct = default)
    {
        await SyncActiveRunsAsync(ct).ConfigureAwait(false);
        var entries = await _index.ListAsync(query, ct).ConfigureAwait(false);
        var runsRoot = Path.GetFullPath(_options.RunsRoot);
        return entries
            .Select(e =>
            {
                var meta = AgentBackendRunMetadataStore.TryRead(runsRoot, e.RunId);
                return new AgentFleetSummary(
                    e.RunId, e.Title, e.Status, e.Stage, e.AgentCount,
                    e.LastActivityAtUtc, e.Pinned, e.Archived,
                    meta?.Backend.ToString() ?? e.BackendKind,
                    meta?.FallbackFrom?.ToString() ?? e.BackendFallbackFrom,
                    e.PrUrl,
                    e.PrNumber,
                    e.CiStatus,
                    e.CiLogsUrl,
                    e.PlaybookHits,
                    e.PlaybookAttempts,
                    e.QualityScore);
            })
            .ToList();
    }

    public async Task<AgentFleetRunDetail?> GetSummaryAsync(Guid runId, CancellationToken ct = default)
    {
        await UpsertFromRunAsync(runId, ct).ConfigureAwait(false);
        var entry = await _index.GetAsync(runId, ct).ConfigureAwait(false);
        if (entry is null)
            return null;

        var runDir = GetRunDir(runId);
        var flow = await _flowProgress.LoadAsync(runId, ct).ConfigureAwait(false);
        return new AgentFleetRunDetail(
            Entry: entry,
            SubagentCount: CountSubdirectories(Path.Combine(runDir, "subagents")),
            DelegationCount: CountJsonFiles(Path.Combine(runDir, "delegations")),
            EvidenceCount: CountEvidence(runDir),
            FlowName: flow?.FlowName,
            CurrentFlowNodeId: flow?.CurrentNodeId,
            LastError: flow?.Nodes?.FirstOrDefault(n => n.NodeId == flow.CurrentNodeId)?.LastError
                       ?? entry.FailureReason);
    }

    public async Task UpsertFromRunAsync(Guid runId, CancellationToken ct = default)
    {
        var existing = await _index.GetAsync(runId, ct).ConfigureAwait(false);
        var run = await _repository.GetAsync(runId, ct).ConfigureAwait(false);
        var live = _runControl.GetRunState(runId);

        if (run is null && live is null && existing is null)
            return;

        var runDir = GetRunDir(runId);
        var ship = _shipState is not null
            ? await _shipState.GetAsync(runId, ct).ConfigureAwait(false)
            : null;
        var status = _shipSync?.ResolveStatus(run, ship) ?? MapStatus(run, live);
        if (ReadHandoffStatus(runDir) is { } handoffStatus)
            status = handoffStatus;

        var stage = live?.CurrentPhase ?? run?.Status.ToString() ?? status.ToString();
        if (status is AgentFleetStatus.HandoffPending)
            stage = "handoff_pending";
        else if (status is AgentFleetStatus.HandoffComplete)
            stage = "handoff_complete";
        var title = existing?.Title
                    ?? run?.Plan?.ApplicationName
                    ?? $"Run {runId.ToString()[..8]}";
        var started = run?.StartedAt ?? live?.StartedAtUtc ?? existing?.StartedAtUtc ?? DateTime.UtcNow;
        var stack = FormatStack(run?.Plan?.TechStack);
        var verifyStatus = ReadVerifyStatus(runDir);
        var agentCount = CountSubdirectories(Path.Combine(runDir, "subagents"))
                         + (live is not null ? 1 : 0);

        var usage = _usageRollup?.Rollup(runId);
        var budgetUsage = _budget?.GetUsage(runId);
        var costUsd = Math.Max(
            usage?.CostUsd ?? 0,
            budgetUsage is null ? 0 : (double)budgetUsage.CostUsdUsed);
        var lastActivity = usage?.LastActivityAtUtc
                           ?? existing?.LastActivityAtUtc
                           ?? DateTime.UtcNow;

        var backendKind = ReadBackendKind(runDir);
        var backendFallbackFrom = ReadBackendFallbackFrom(runDir);
        var playbookStats = RunPlaybookStats.Read(runDir);
        var quality = FleetRunQualityCalculator.Compute(run, runDir, playbookStats);

        var entry = new AgentFleetEntry(
            RunId: runId,
            Title: title,
            SpaceId: existing?.SpaceId,
            Status: status,
            Stage: stage,
            AgentCount: agentCount,
            StartedAtUtc: started,
            LastActivityAtUtc: lastActivity,
            CostUsd: costUsd,
            ModelProfile: existing?.ModelProfile,
            VerifyStatus: verifyStatus,
            Stack: stack ?? existing?.Stack,
            Pinned: existing?.Pinned ?? false,
            Archived: existing?.Archived ?? false,
            FailureReason: run?.FailureReason ?? existing?.FailureReason,
            BackendKind: backendKind,
            BackendFallbackFrom: backendFallbackFrom,
            PrUrl: ship?.PullRequestUrl ?? existing?.PrUrl,
            PrNumber: ship?.PullRequestNumber ?? existing?.PrNumber,
            CiStatus: ship?.CiStatus ?? existing?.CiStatus,
            CiLogsUrl: ship?.CiLogsUrl ?? existing?.CiLogsUrl,
            PlaybookHits: playbookStats.Hits,
            PlaybookAttempts: playbookStats.Attempts,
            QualityScore: quality.Score);

        await _index.UpsertAsync(entry, ct).ConfigureAwait(false);

        if (_sessionSearch is not null)
        {
            var filesTouched = run?.Files.Count > 0
                ? string.Join(' ', run.Files.Take(40).Select(f => f.RelativePath))
                : null;
            var indexDoc = new FleetSessionIndexDocument(
                runId,
                title,
                run?.UserRequest,
                run?.FailureReason ?? entry.FailureReason,
                filesTouched,
                entry.SpaceId,
                entry.Stack,
                SqliteFleetSessionSearchService.ToOutcome(entry.Status),
                entry.LastActivityAtUtc,
                entry.Pinned);
            await _sessionSearch.IndexAsync(indexDoc, ct).ConfigureAwait(false);
            if (_similarRuns is not null)
                await _similarRuns.IndexAsync(indexDoc, ct).ConfigureAwait(false);
        }

        if (existing?.Status != status || !string.Equals(existing.Stage, stage, StringComparison.Ordinal))
        {
            var evt = new AgentFleetStatusEvent(runId, status, stage, DateTime.UtcNow);
            AgentFleetTelemetry.RecordTransition(status, stage);
            if (status == AgentFleetStatus.Verifying && !_verifyStartedAt.ContainsKey(runId))
            {
                _verifyStartedAt[runId] = DateTime.UtcNow;
                var elapsed = (DateTime.UtcNow - started).TotalSeconds;
                if (elapsed >= 0)
                    AgentFleetTelemetry.RecordTimeToVerify(elapsed, runId);
            }

            if (StatusChanged is not null)
                await StatusChanged.Invoke(evt).ConfigureAwait(false);
            if (_eventHub is not null)
                await _eventHub.PublishAsync(evt, ct).ConfigureAwait(false);
        }
    }

    public async Task BulkArchiveAsync(AgentFleetBulkArchiveRequest request, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, request.OlderThanDays));
        var entries = await _index.ListAsync(new AgentFleetListQuery(IncludeArchived: false, Limit: 500), ct)
            .ConfigureAwait(false);

        foreach (var entry in entries)
        {
            if (request.RunIds is { Count: > 0 } ids && !ids.Contains(entry.RunId))
                continue;

            if (entry.Status is not (AgentFleetStatus.Completed or AgentFleetStatus.Failed or AgentFleetStatus.Cancelled))
                continue;

            if (request.RunIds is null or { Count: 0 } && entry.LastActivityAtUtc > cutoff)
                continue;

            await PatchAsync(entry.RunId, new AgentFleetPatchRequest(Archived: true, Actor: request.Actor), ct)
                .ConfigureAwait(false);
        }
    }

    public Task WriteAuditAsync(string action, Guid runId, string? actor, CancellationToken ct = default)
    {
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(_options.IndexDbPath))!;
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "agent-fleet-audit.jsonl");
            var line = JsonSerializer.Serialize(new
            {
                action,
                runId,
                actor,
                timestampUtc = DateTime.UtcNow
            });
            return File.AppendAllTextAsync(path, line + Environment.NewLine, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to write fleet audit for {RunId}", runId);
            return Task.CompletedTask;
        }
    }

    public async Task PatchAsync(Guid runId, AgentFleetPatchRequest patch, CancellationToken ct = default)
    {
        if (patch.StatusOverride is { } statusOverride && _shipState is not null)
        {
            var ship = await _shipState.GetAsync(runId, ct).ConfigureAwait(false)
                       ?? new RunShipState(runId, null, null, null, FleetCiStatus.None, null, DateTime.UtcNow);
            ship = ship with { ManualStatusOverride = statusOverride, UpdatedAtUtc = DateTime.UtcNow };
            await _shipState.SaveAsync(ship, ct).ConfigureAwait(false);
            await UpsertFromRunAsync(runId, ct).ConfigureAwait(false);
        }

        await _index.PatchAsync(runId, patch, ct).ConfigureAwait(false);
    }

    public async Task<int> RebuildIndexAsync(CancellationToken ct = default)
    {
        var count = 0;
        var runs = await _repository.ListAsync(ct).ConfigureAwait(false);
        foreach (var run in runs)
        {
            await UpsertFromRunAsync(run.Id, ct).ConfigureAwait(false);
            count++;
        }

        var runsRoot = Path.GetFullPath(_options.RunsRoot);
        if (Directory.Exists(runsRoot))
        {
            foreach (var dir in Directory.EnumerateDirectories(runsRoot))
            {
                if (!Guid.TryParse(Path.GetFileName(dir), out var runId))
                    continue;
                if (runs.All(r => r.Id != runId))
                {
                    await UpsertFromRunAsync(runId, ct).ConfigureAwait(false);
                    count++;
                }
            }
        }

        _logger.LogInformation("Rebuilt agent fleet index with {Count} runs", count);
        return count;
    }

    private async Task SyncActiveRunsAsync(CancellationToken ct)
    {
        foreach (var active in _runControl.GetHealthSnapshot().Active)
            await UpsertFromRunAsync(active.RunId, ct).ConfigureAwait(false);
    }

    private string GetRunDir(Guid runId) =>
        Path.Combine(Path.GetFullPath(_options.RunsRoot), runId.ToString("D"));

    private static AgentFleetStatus MapStatus(AppGenerationOrchestrator? run, AutonomousRunStateSnapshot? live)
    {
        if (live?.IsCancellationRequested == true)
            return AgentFleetStatus.Cancelled;

        if (live is not null)
        {
            return live.CurrentPhase.ToLowerInvariant() switch
            {
                var p when p.Contains("verify", StringComparison.Ordinal) => AgentFleetStatus.Verifying,
                var p when p.Contains("repair", StringComparison.Ordinal) || p.Contains("fix", StringComparison.Ordinal) => AgentFleetStatus.Repairing,
                var p when p.Contains("plan", StringComparison.Ordinal) => AgentFleetStatus.Planning,
                var p when p.Contains("generat", StringComparison.Ordinal) => AgentFleetStatus.Generating,
                var p when p.Contains("ship", StringComparison.Ordinal) => AgentFleetStatus.PrReady,
                var p when p.Contains("approval", StringComparison.Ordinal) => AgentFleetStatus.WaitingForApproval,
                var p when p.Contains("ci", StringComparison.Ordinal) => AgentFleetStatus.WaitingForCi,
                _ when live.IsPaused => AgentFleetStatus.WaitingForApproval,
                _ => AgentFleetStatus.Generating
            };
        }

        if (run is null)
            return AgentFleetStatus.Queued;

        return run.Status switch
        {
            GenerationStatus.Planning => AgentFleetStatus.Planning,
            GenerationStatus.Generating => AgentFleetStatus.Generating,
            GenerationStatus.Testing => AgentFleetStatus.Verifying,
            GenerationStatus.Fixing => AgentFleetStatus.Repairing,
            GenerationStatus.Completed => AgentFleetStatus.Completed,
            GenerationStatus.Failed when run.FailureReason?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true
                => AgentFleetStatus.Cancelled,
            GenerationStatus.Failed => AgentFleetStatus.Failed,
            _ => AgentFleetStatus.Queued
        };
    }

    private static string? FormatStack(TechStack? stack)
    {
        if (stack is null) return null;
        var parts = new List<string>();
        if (stack.Languages?.Count > 0) parts.AddRange(stack.Languages);
        if (stack.Frameworks?.Count > 0) parts.AddRange(stack.Frameworks);
        return parts.Count > 0 ? string.Join("/", parts.Take(3)) : null;
    }

    private static int CountSubdirectories(string path) =>
        Directory.Exists(path) ? Directory.EnumerateDirectories(path).Count() : 0;

    private static int CountJsonFiles(string path) =>
        Directory.Exists(path)
            ? Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly).Count()
            : 0;

    private static int CountEvidence(string runDir)
    {
        var obscura = Path.Combine(runDir, "obscura");
        var verify = Path.Combine(runDir, "verify");
        var count = 0;
        if (Directory.Exists(obscura))
            count += Directory.EnumerateFiles(obscura, "*", SearchOption.AllDirectories).Count();
        if (Directory.Exists(verify))
            count += Directory.EnumerateFiles(verify, "*", SearchOption.AllDirectories).Count();
        return count;
    }

    private static string? ReadBackendFallbackFrom(string runDir)
    {
        var runsRoot = Path.GetDirectoryName(runDir);
        if (runsRoot is null)
            return null;

        var runIdStr = Path.GetFileName(runDir);
        if (!Guid.TryParse(runIdStr, out var runId))
            return null;

        return AgentBackendRunMetadataStore.TryRead(runsRoot, runId)?.FallbackFrom?.ToString();
    }

    private static string? ReadBackendKind(string runDir)
    {
        var path = Path.Combine(runDir, "handoff", "backend.json");
        if (!File.Exists(path))
            return AgentBackendKind.Libr4Native.ToString();

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("backend", out var backendEl)
                ? backendEl.GetString()
                : AgentBackendKind.Libr4Native.ToString();
        }
        catch
        {
            return AgentBackendKind.Libr4Native.ToString();
        }
    }

    private static AgentFleetStatus? ReadHandoffStatus(string runDir)
    {
        var path = Path.Combine(runDir, "handoff", "promote-state.json");
        if (!File.Exists(path))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("status", out var statusEl))
                return null;

            return statusEl.GetString() switch
            {
                "HandoffPending" => AgentFleetStatus.HandoffPending,
                "HandoffComplete" => AgentFleetStatus.HandoffComplete,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadVerifyStatus(string runDir)
    {
        var manifest = Path.Combine(runDir, "verify", "manifest.json");
        if (!File.Exists(manifest))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(manifest));
            if (doc.RootElement.TryGetProperty("status", out var status))
                return status.GetString();
            if (doc.RootElement.TryGetProperty("passed", out var passed))
                return passed.GetBoolean() ? "pass" : "fail";
        }
        catch
        {
            // ignore malformed manifest
        }
        return null;
    }
}
