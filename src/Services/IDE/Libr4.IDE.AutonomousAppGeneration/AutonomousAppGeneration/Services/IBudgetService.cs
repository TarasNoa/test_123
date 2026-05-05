using System.Collections.Concurrent;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// P1-5 of audit roadmap. Per-run / per-tenant LLM token + USD budget enforcement.
/// Stages call <see cref="TryConsumeAsync"/> before issuing an LLM request and abort
/// gracefully when the budget would be exceeded. Default in-memory implementation;
/// production should swap for a persistent + distributed quota tracker (see P2-5).
/// </summary>
public interface IBudgetService
{
    /// <summary>
    /// Reserves <paramref name="estimatedTokens"/> for <paramref name="runId"/> if the run is still
    /// within budget. Returns false when the request would push over the cap.
    /// </summary>
    Task<BudgetReservation> TryConsumeAsync(
        Guid runId,
        string stage,
        long estimatedTokens,
        decimal estimatedCostUsd,
        CancellationToken ct = default);

    /// <summary>Snapshot of current usage for a run.</summary>
    BudgetUsage GetUsage(Guid runId);

    /// <summary>Resets usage on terminal state. Idempotent.</summary>
    void Release(Guid runId);
}

public sealed record BudgetReservation(
    bool Granted,
    long ReservedTokens,
    decimal ReservedCostUsd,
    long RemainingTokens,
    decimal RemainingCostUsd,
    string? DenialReason = null);

public sealed record BudgetUsage(
    Guid RunId,
    long TokensUsed,
    decimal CostUsdUsed,
    int RequestsIssued);

public sealed class BudgetOptions
{
    /// <summary>Per-run token cap. Zero / negative disables enforcement.</summary>
    public long PerRunTokenCap { get; set; } = 1_000_000;

    /// <summary>Per-run USD cap. Zero / negative disables enforcement.</summary>
    public decimal PerRunCostUsdCap { get; set; } = 5.00m;

    /// <summary>Per-stage token cap (multiplier of total). 1.0 = full budget on a single stage.</summary>
    public double PerStageFraction { get; set; } = 0.6;

    // P2-5: per-day and per-tenant caps.

    /// <summary>Per-day (calendar day UTC) token cap across all runs. Zero / negative disables.</summary>
    public long PerDayTokenCap { get; set; } = 50_000_000;

    /// <summary>Per-day USD cap across all runs. Zero / negative disables.</summary>
    public decimal PerDayCostUsdCap { get; set; } = 100.00m;

    /// <summary>Per-tenant token cap per day. Zero / negative disables.</summary>
    public long PerTenantDayTokenCap { get; set; } = 10_000_000;

    /// <summary>Per-tenant USD cap per day. Zero / negative disables.</summary>
    public decimal PerTenantDayCostUsdCap { get; set; } = 20.00m;
}

/// <summary>
/// Thread-safe in-memory budget tracker. Suitable for a single-host deployment;
/// for multi-host see P2-5 (distributed quota via Redis / Postgres).
/// Supports per-run, per-day and per-tenant daily caps.
/// </summary>
public sealed class InMemoryBudgetService : IBudgetService
{
    private readonly BudgetOptions _options;
    private readonly ConcurrentDictionary<Guid, RunQuota> _quotas = new();

    // P2-5: daily aggregate and per-tenant daily tracking.
    private readonly object _daySync = new();
    private DateOnly _currentDay = DateOnly.FromDateTime(DateTime.UtcNow);
    private long _dayTokens;
    private decimal _dayCost;
    private readonly ConcurrentDictionary<string, TenantDayQuota> _tenantQuotas = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryBudgetService(BudgetOptions? options = null)
    {
        _options = options ?? new BudgetOptions();
    }

    public Task<BudgetReservation> TryConsumeAsync(
        Guid runId,
        string stage,
        long estimatedTokens,
        decimal estimatedCostUsd,
        CancellationToken ct = default)
        => TryConsumeAsync(runId, stage, estimatedTokens, estimatedCostUsd, tenantId: null, ct);

    /// <summary>Overload that accepts an optional <paramref name="tenantId"/> for per-tenant cap enforcement.</summary>
    public Task<BudgetReservation> TryConsumeAsync(
        Guid runId,
        string stage,
        long estimatedTokens,
        decimal estimatedCostUsd,
        string? tenantId,
        CancellationToken ct = default)
    {
        if (estimatedTokens < 0) estimatedTokens = 0;
        if (estimatedCostUsd < 0) estimatedCostUsd = 0;

        var quota = _quotas.GetOrAdd(runId, _ => new RunQuota());

        lock (quota.Sync)
        {
            // Per-run token cap.
            if (_options.PerRunTokenCap > 0 && quota.Tokens + estimatedTokens > _options.PerRunTokenCap)
                return Task.FromResult(Denied(
                    $"per_run_token_cap_exceeded:{_options.PerRunTokenCap}",
                    Math.Max(0, _options.PerRunTokenCap - quota.Tokens),
                    Math.Max(0m, _options.PerRunCostUsdCap - quota.Cost)));

            // Per-run USD cap.
            if (_options.PerRunCostUsdCap > 0 && quota.Cost + estimatedCostUsd > _options.PerRunCostUsdCap)
                return Task.FromResult(Denied(
                    $"per_run_cost_cap_exceeded:{_options.PerRunCostUsdCap:F2}",
                    Math.Max(0, _options.PerRunTokenCap - quota.Tokens),
                    Math.Max(0m, _options.PerRunCostUsdCap - quota.Cost)));

            // Per-day aggregate caps.
            var denied = CheckAndUpdateDayCap(estimatedTokens, estimatedCostUsd);
            if (denied is not null) return Task.FromResult(denied);

            // Per-tenant daily caps.
            if (tenantId is not null)
            {
                denied = CheckAndUpdateTenantCap(tenantId, estimatedTokens, estimatedCostUsd);
                if (denied is not null) return Task.FromResult(denied);
            }

            quota.Tokens += estimatedTokens;
            quota.Cost += estimatedCostUsd;
            quota.Requests += 1;

            return Task.FromResult(new BudgetReservation(
                Granted: true,
                ReservedTokens: estimatedTokens,
                ReservedCostUsd: estimatedCostUsd,
                RemainingTokens: Math.Max(0, _options.PerRunTokenCap - quota.Tokens),
                RemainingCostUsd: Math.Max(0m, _options.PerRunCostUsdCap - quota.Cost)));
        }
    }

    public BudgetUsage GetUsage(Guid runId)
    {
        if (!_quotas.TryGetValue(runId, out var quota))
            return new BudgetUsage(runId, 0, 0, 0);
        lock (quota.Sync)
            return new BudgetUsage(runId, quota.Tokens, quota.Cost, quota.Requests);
    }

    public void Release(Guid runId) => _quotas.TryRemove(runId, out _);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static BudgetReservation Denied(string reason, long remTokens, decimal remCost) =>
        new(Granted: false, ReservedTokens: 0, ReservedCostUsd: 0,
            RemainingTokens: remTokens, RemainingCostUsd: remCost, DenialReason: reason);

    private BudgetReservation? CheckAndUpdateDayCap(long tokens, decimal cost)
    {
        lock (_daySync)
        {
            RollDayIfNeeded();

            if (_options.PerDayTokenCap > 0 && _dayTokens + tokens > _options.PerDayTokenCap)
                return Denied($"per_day_token_cap_exceeded:{_options.PerDayTokenCap}",
                    Math.Max(0, _options.PerDayTokenCap - _dayTokens),
                    Math.Max(0m, _options.PerDayCostUsdCap - _dayCost));

            if (_options.PerDayCostUsdCap > 0 && _dayCost + cost > _options.PerDayCostUsdCap)
                return Denied($"per_day_cost_cap_exceeded:{_options.PerDayCostUsdCap:F2}",
                    Math.Max(0, _options.PerDayTokenCap - _dayTokens),
                    Math.Max(0m, _options.PerDayCostUsdCap - _dayCost));

            _dayTokens += tokens;
            _dayCost += cost;
            return null;
        }
    }

    private BudgetReservation? CheckAndUpdateTenantCap(string tenantId, long tokens, decimal cost)
    {
        var tq = _tenantQuotas.GetOrAdd(tenantId, _ => new TenantDayQuota());
        lock (tq.Sync)
        {
            RollTenantDayIfNeeded(tq);

            if (_options.PerTenantDayTokenCap > 0 && tq.Tokens + tokens > _options.PerTenantDayTokenCap)
                return Denied($"per_tenant_day_token_cap_exceeded:{_options.PerTenantDayTokenCap}",
                    Math.Max(0, _options.PerTenantDayTokenCap - tq.Tokens),
                    Math.Max(0m, _options.PerTenantDayCostUsdCap - tq.Cost));

            if (_options.PerTenantDayCostUsdCap > 0 && tq.Cost + cost > _options.PerTenantDayCostUsdCap)
                return Denied($"per_tenant_day_cost_cap_exceeded:{_options.PerTenantDayCostUsdCap:F2}",
                    Math.Max(0, _options.PerTenantDayTokenCap - tq.Tokens),
                    Math.Max(0m, _options.PerTenantDayCostUsdCap - tq.Cost));

            tq.Tokens += tokens;
            tq.Cost += cost;
            return null;
        }
    }

    private void RollDayIfNeeded()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today <= _currentDay) return;
        _currentDay = today;
        _dayTokens = 0;
        _dayCost = 0;
    }

    private static void RollTenantDayIfNeeded(TenantDayQuota tq)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today <= tq.Day) return;
        tq.Day = today;
        tq.Tokens = 0;
        tq.Cost = 0;
    }

    private sealed class RunQuota
    {
        public readonly object Sync = new();
        public long Tokens;
        public decimal Cost;
        public int Requests;
    }

    private sealed class TenantDayQuota
    {
        public readonly object Sync = new();
        public DateOnly Day = DateOnly.FromDateTime(DateTime.UtcNow);
        public long Tokens;
        public decimal Cost;
    }
}
