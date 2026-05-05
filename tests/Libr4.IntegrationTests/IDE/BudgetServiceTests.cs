using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class BudgetServiceTests
{
    [Fact]
    public async Task TryConsume_WithinTokenCap_GrantsReservation()
    {
        var sut = new InMemoryBudgetService(new BudgetOptions
        {
            PerRunTokenCap = 10_000,
            PerRunCostUsdCap = 1.00m
        });
        var runId = Guid.NewGuid();

        var reservation = await sut.TryConsumeAsync(runId, "planning", 4_000, 0.10m);

        reservation.Granted.Should().BeTrue();
        reservation.ReservedTokens.Should().Be(4_000);
        reservation.RemainingTokens.Should().Be(6_000);
    }

    [Fact]
    public async Task TryConsume_OverTokenCap_DeniesAndRecordsReason()
    {
        var sut = new InMemoryBudgetService(new BudgetOptions
        {
            PerRunTokenCap = 5_000,
            PerRunCostUsdCap = 10m
        });
        var runId = Guid.NewGuid();

        await sut.TryConsumeAsync(runId, "planning", 4_000, 0.10m);
        var second = await sut.TryConsumeAsync(runId, "generation", 3_000, 0.05m);

        second.Granted.Should().BeFalse();
        second.DenialReason.Should().Contain("per_run_token_cap_exceeded");
    }

    [Fact]
    public async Task TryConsume_OverUsdCap_Denies()
    {
        var sut = new InMemoryBudgetService(new BudgetOptions
        {
            PerRunTokenCap = 1_000_000,
            PerRunCostUsdCap = 0.50m
        });
        var runId = Guid.NewGuid();

        await sut.TryConsumeAsync(runId, "planning", 1_000, 0.40m);
        var second = await sut.TryConsumeAsync(runId, "generation", 1_000, 0.20m);

        second.Granted.Should().BeFalse();
        second.DenialReason.Should().Contain("per_run_cost_cap_exceeded");
    }

    [Fact]
    public async Task GetUsage_ReportsTotals()
    {
        var sut = new InMemoryBudgetService();
        var runId = Guid.NewGuid();
        await sut.TryConsumeAsync(runId, "planning", 1_500, 0.10m);
        await sut.TryConsumeAsync(runId, "generation", 2_500, 0.20m);

        var usage = sut.GetUsage(runId);

        usage.TokensUsed.Should().Be(4_000);
        usage.CostUsdUsed.Should().Be(0.30m);
        usage.RequestsIssued.Should().Be(2);
    }

    [Fact]
    public async Task Release_ResetsUsage()
    {
        var sut = new InMemoryBudgetService();
        var runId = Guid.NewGuid();
        await sut.TryConsumeAsync(runId, "planning", 100, 0.01m);
        sut.Release(runId);

        sut.GetUsage(runId).TokensUsed.Should().Be(0);
    }

    [Fact]
    public async Task TryConsume_DisabledCaps_AlwaysGranted()
    {
        var sut = new InMemoryBudgetService(new BudgetOptions
        {
            PerRunTokenCap = 0,
            PerRunCostUsdCap = 0m,
            PerDayTokenCap = 0,
            PerDayCostUsdCap = 0m,
            PerTenantDayTokenCap = 0,
            PerTenantDayCostUsdCap = 0m
        });

        var reservation = await sut.TryConsumeAsync(Guid.NewGuid(), "planning", long.MaxValue / 2, 1_000_000m);

        reservation.Granted.Should().BeTrue();
    }

    [Fact]
    public async Task TryConsume_IsThreadSafe()
    {
        var sut = new InMemoryBudgetService(new BudgetOptions
        {
            PerRunTokenCap = 100_000,
            PerRunCostUsdCap = 100m
        });
        var runId = Guid.NewGuid();

        await Task.WhenAll(Enumerable.Range(0, 50).Select(i =>
            sut.TryConsumeAsync(runId, $"stage_{i}", 100, 0.01m)));

        var usage = sut.GetUsage(runId);
        usage.TokensUsed.Should().Be(50 * 100);
        usage.RequestsIssued.Should().Be(50);
    }
}
