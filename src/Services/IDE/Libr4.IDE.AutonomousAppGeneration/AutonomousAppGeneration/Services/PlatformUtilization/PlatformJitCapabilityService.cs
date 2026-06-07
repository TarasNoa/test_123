using System.Collections.Concurrent;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

public interface IPlatformJitJsonlAudit
{
    Task WriteInjectedAsync(PlatformJitInjectedEvent entry, CancellationToken ct = default);
    Task WriteResolvedAsync(PlatformJitResolvedEvent entry, CancellationToken ct = default);
}

public sealed class PlatformJitJsonlAudit : IPlatformJitJsonlAudit
{
    private readonly AgentRuntimeOptions _runtimeOptions;
    private readonly object _lock = new();

    public PlatformJitJsonlAudit(IOptions<AgentRuntimeOptions> runtimeOptions) =>
        _runtimeOptions = runtimeOptions.Value;

    public Task WriteInjectedAsync(PlatformJitInjectedEvent entry, CancellationToken ct = default)
    {
        Append(entry.RunId, entry);
        return Task.CompletedTask;
    }

    public Task WriteResolvedAsync(PlatformJitResolvedEvent entry, CancellationToken ct = default)
    {
        Append(entry.RunId, entry);
        return Task.CompletedTask;
    }

    private void Append(Guid runId, object payload)
    {
        var dir = Path.Combine(_runtimeOptions.RunsRoot, runId.ToString("D"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "platform-jit-audit.jsonl");
        var line = JsonSerializer.Serialize(payload);
        lock (_lock)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}

public interface IPlatformJitCapabilityService
{
    Task<PlatformJitInjectionResult> TryInjectForRepairAsync(
        Guid runId,
        int iteration,
        int repairAttempt,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog,
        GenerationPlan plan,
        CancellationToken ct = default);

    Task TryMarkResolvedAsync(Guid runId, int iteration, CancellationToken ct = default);
}

internal sealed class PlatformJitPendingEntry
{
    public required string InjectionId { get; init; }
    public required int InjectedAtIteration { get; init; }
    public required string PlaybookId { get; init; }
    public required string ErrorSignature { get; init; }
}

public sealed class PlatformJitCapabilityService : IPlatformJitCapabilityService
{
    private static readonly ConcurrentDictionary<Guid, PlatformJitPendingEntry> Pending = new();

    private readonly RepairPlaybookService _learnedPlaybook;
    private readonly IPlatformJitJsonlAudit _audit;
    private readonly AutonomousPlatformUtilizationOptions _options;
    private readonly ILogger<PlatformJitCapabilityService> _logger;

    public PlatformJitCapabilityService(
        RepairPlaybookService learnedPlaybook,
        IPlatformJitJsonlAudit audit,
        IOptions<AutonomousPlatformUtilizationOptions> options,
        ILogger<PlatformJitCapabilityService> logger)
    {
        _learnedPlaybook = learnedPlaybook;
        _audit = audit;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PlatformJitInjectionResult> TryInjectForRepairAsync(
        Guid runId,
        int iteration,
        int repairAttempt,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog,
        GenerationPlan plan,
        CancellationToken ct = default)
    {
        if (!_options.EnableOrchestratorJitInjection || errors.Count == 0)
            return new PlatformJitInjectionResult(false, null, null, null, null);

        var catalog = PlatformJitPlaybookCatalog.TryMatch(errors, buildLog, plan);
        var learnedHint = _options.EnableOrchestratorJitLearnedPlaybook
            ? await _learnedPlaybook.TryGetHintAsync(errors, buildLog, plan, ct).ConfigureAwait(false)
            : null;

        if (catalog is null && string.IsNullOrWhiteSpace(learnedHint))
            return new PlatformJitInjectionResult(false, null, null, null, null);

        var playbookId = catalog?.PlaybookId ?? "learned_repair_playbook";
        var signature = catalog?.ErrorSignature
                        ?? RepairPlaybookSignature.FromErrors(errors, buildLog, plan).Signature;

        var sb = new System.Text.StringBuilder();
        if (catalog is not null)
            sb.AppendLine(catalog.InjectionText.Trim());
        if (!string.IsNullOrWhiteSpace(learnedHint))
        {
            sb.AppendLine();
            sb.AppendLine("LEARNED PLAYBOOK (prior successful repair for this signature):");
            sb.AppendLine(learnedHint.Trim());
        }

        var injectionId = Guid.NewGuid().ToString("D");
        var text = sb.ToString().Trim();

        Pending[runId] = new PlatformJitPendingEntry
        {
            InjectionId = injectionId,
            InjectedAtIteration = iteration,
            PlaybookId = playbookId,
            ErrorSignature = signature
        };

        PlatformCapabilityBriefingScope.SetJitOverlay(text);

        await _audit.WriteInjectedAsync(new PlatformJitInjectedEvent(
            Event: "injected",
            InjectionId: injectionId,
            RunId: runId,
            RepairAttempt: repairAttempt,
            Iteration: iteration,
            JitInjected: true,
            PlaybookId: playbookId,
            ErrorSignature: signature,
            TimestampUtc: DateTime.UtcNow), ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[AutoGen {RunId}] Orchestrator JIT injected playbook={Playbook} signature={Signature} iter={Iteration}",
            runId,
            playbookId,
            signature,
            iteration);

        return new PlatformJitInjectionResult(true, injectionId, playbookId, signature, text);
    }

    public async Task TryMarkResolvedAsync(Guid runId, int iteration, CancellationToken ct = default)
    {
        if (!Pending.TryRemove(runId, out var pending))
            return;

        var resolvedWithinNext = iteration > pending.InjectedAtIteration;

        await _audit.WriteResolvedAsync(new PlatformJitResolvedEvent(
            Event: "resolved",
            InjectionId: pending.InjectionId,
            RunId: runId,
            ResolvedAtIteration: iteration,
            ResolvedWithinNextIteration: resolvedWithinNext,
            TimestampUtc: DateTime.UtcNow), ct).ConfigureAwait(false);

        PlatformCapabilityBriefingScope.ClearJitOverlay();

        _logger.LogInformation(
            "[AutoGen {RunId}] Orchestrator JIT resolved playbook={Playbook} withinNext={WithinNext} at iter={Iteration}",
            runId,
            pending.PlaybookId,
            resolvedWithinNext,
            iteration);
    }
}
