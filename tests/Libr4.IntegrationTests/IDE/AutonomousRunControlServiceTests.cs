using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AutonomousRunControlServiceTests
{
    [Fact]
    public void PauseResumeCancel_Flow_IsTrackedCorrectly()
    {
        var runControl = new AutonomousRunControlService();
        var runId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        runControl.RegisterRun(runId, cts);

        runControl.UpdateRunProgress(runId, "testing", 2, 3);

        runControl.PauseRun(runId).Should().BeTrue();
        runControl.GetRunState(runId)!.IsPaused.Should().BeTrue();

        runControl.ResumeRun(runId).Should().BeTrue();
        runControl.GetRunState(runId)!.IsPaused.Should().BeFalse();

        runControl.CancelRun(runId, "qa-bot", "manual_stop").Should().BeTrue();
        var state = runControl.GetRunState(runId);
        state.Should().NotBeNull();
        state!.IsCancellationRequested.Should().BeTrue();
        state.CurrentPhase.Should().Be("testing");
        state.CurrentIteration.Should().Be(2);
        state.CurrentAttempt.Should().Be(3);
        state.CancelMetadata.Should().NotBeNull();
        state.CancelMetadata!.Actor.Should().Be("qa-bot");
        state.CancelMetadata.Reason.Should().Be("manual_stop");
    }

    [Fact]
    public void CompleteRun_RemovesFromActiveAndUpdatesTotals()
    {
        var runControl = new AutonomousRunControlService();
        var runId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        runControl.RegisterRun(runId, cts);

        runControl.CancelRun(runId, "user", "cancelled_by_request").Should().BeTrue();
        runControl.CompleteRun(runId, "Failed", "cancelled_by_request");

        runControl.GetRunState(runId).Should().BeNull();
        var health = runControl.GetHealthSnapshot();
        health.ActiveRuns.Should().Be(0);
        health.TotalStarted.Should().Be(1);
        health.TotalCompleted.Should().Be(1);
        health.TotalCancelled.Should().Be(1);
    }
}
