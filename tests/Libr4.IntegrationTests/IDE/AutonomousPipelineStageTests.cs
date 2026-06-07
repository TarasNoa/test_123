using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AutonomousPipelineStageTests
{
    [Fact]
    public void RecordPipelineStageReached_advances_monotonically()
    {
        var orchestrator = AppGenerationOrchestrator.Create("req", "fp");

        orchestrator.RecordPipelineStageReached(AutonomousPipelineStages.Planning);
        orchestrator.RecordPipelineStageReached(AutonomousPipelineStages.Generation);
        orchestrator.RecordPipelineStageReached(AutonomousPipelineStages.Planning);

        Assert.Equal(AutonomousPipelineStages.Generation, orchestrator.PipelineStageReached);

        orchestrator.RecordPipelineStageReached(AutonomousPipelineStages.RepairLoop);
        Assert.Equal(AutonomousPipelineStages.RepairLoop, orchestrator.PipelineStageReached);

        orchestrator.MarkCompleted();
        Assert.Equal(AutonomousPipelineStages.Completed, orchestrator.PipelineStageReached);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("Planning", false)]
    [InlineData("Generation", false)]
    [InlineData("StartupBuild", true)]
    [InlineData("RepairLoop", true)]
    [InlineData("Completed", true)]
    public void IsRecoveryMeasurementEligible_requires_startup_build_or_later(string? stage, bool expected) =>
        Assert.Equal(expected, AutonomousPipelineStages.IsRecoveryMeasurementEligible(stage));
}
