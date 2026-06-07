using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Crystallization;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

/// <summary>Claude Code-style learned fix hints from prior successful repairs.</summary>
public sealed class RepairPlaybookService
{
    private readonly IRepairPlaybookStore _store;
    private readonly ISkillCrystallizer? _crystallizer;

    public RepairPlaybookService(IRepairPlaybookStore store, ISkillCrystallizer? crystallizer = null)
    {
        _store = store;
        _crystallizer = crystallizer;
    }

    public Task<string?> TryGetHintAsync(
        IReadOnlyList<ErrorReport> errors,
        string? buildLog,
        GenerationPlan? plan,
        CancellationToken ct = default)
    {
        var signature = RepairPlaybookSignature.FromErrors(errors, buildLog, plan);
        return _store.TryGetHintAsync(signature.Signature, ct);
    }

    public Task<string?> TryGetHintAsync(string errorSignature, CancellationToken ct = default) =>
        _store.TryGetHintAsync(errorSignature, ct);

    public async Task RecordOutcomeAsync(
        IReadOnlyList<ErrorReport> errors,
        string? buildLog,
        GenerationPlan? plan,
        string fixPattern,
        bool succeeded,
        CancellationToken ct = default)
    {
        var signature = RepairPlaybookSignature.FromErrors(errors, buildLog, plan);
        await RecordOutcomeAsync(signature.Signature, fixPattern, succeeded, signature.StackPattern, ct)
            .ConfigureAwait(false);
    }

    public async Task RecordOutcomeAsync(
        string errorSignature,
        string fixPattern,
        bool succeeded,
        string? stackPattern = null,
        CancellationToken ct = default)
    {
        await _store.RecordOutcomeAsync(errorSignature, fixPattern, succeeded, stackPattern, ct).ConfigureAwait(false);
        if (!succeeded || _crystallizer is null)
            return;

        var entry = await _store.GetBySignatureAsync(errorSignature, ct).ConfigureAwait(false);
        if (entry is not null)
            await _crystallizer.TryCrystallizeAsync(entry, ct).ConfigureAwait(false);
    }

    public async Task<string> ExportStatsAsync(CancellationToken ct = default)
    {
        var rows = await _store.ListTopAsync(20, ct).ConfigureAwait(false);
        var payload = rows.Select(entry => new
        {
            signature = entry.ErrorSignature,
            stack = entry.StackPattern,
            fix = entry.FixPattern,
            success_count = entry.SuccessCount,
            fail_count = entry.FailCount,
            score = Math.Round(entry.Score, 2),
            last_used_at = entry.LastUsedAtUtc
        });

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }
}
