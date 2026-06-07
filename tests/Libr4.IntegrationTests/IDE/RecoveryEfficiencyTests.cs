using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RecoveryEfficiencyTests
{
    [Fact]
    public void Mapper_MapsPomDuplicateToConfiguration()
    {
        var category = RecoveryRootCauseMapper.FromClassifier(
            RepairErrorClassifier.RepairErrorClass.PomSyntax,
            "backend/pom.xml",
            "Duplicated tag build");
        category.Should().Be(RecoveryRootCauseCategory.Configuration);
    }

    [Fact]
    public void Aggregator_ComputesMechanismShares()
    {
        var orchestrator = AppGenerationOrchestrator.Create("app", "fp-recovery-eff");
        orchestrator.RecordRecoveryEfficiency(new RecoveryEfficiencyRecord(
            1,
            RecoveryRootCauseCategory.Configuration,
            "PomSyntax",
            RecoveryMechanism.DeterministicStructural,
            2,
            true,
            DateTime.UtcNow));
        orchestrator.RecordRecoveryEfficiency(new RecoveryEfficiencyRecord(
            2,
            RecoveryRootCauseCategory.Imports,
            "CompileSymbol",
            RecoveryMechanism.Llm,
            5,
            false,
            DateTime.UtcNow));

        var report = RecoveryEfficiencyAggregator.BuildReport(orchestrator);
        report.TotalAttempts.Should().Be(2);
        report.ResolvedAttempts.Should().Be(1);
        report.FailedAttempts.Should().Be(1);
        report.ByMechanism.Should().Contain(m => m.Mechanism == "DeterministicStructural" && m.Attempts == 1);
        report.ByMechanism.Should().Contain(m => m.Mechanism == "Llm" && m.Attempts == 1);
        report.LlmAttemptShare.Should().Be(0.5);
        report.RecoverySource.Should().Contain(s => s.Source == "Deterministic");
        report.LlmStats.Invoked.Should().Be(1);
        report.FirstFailure.Should().NotBeNull();
        report.FirstFailure!.ErrorClass.Should().Be("PomSyntax");
    }

    [Fact]
    public void Aggregator_FlagsZeroPatchBottleneckInInsight()
    {
        var orchestrator = AppGenerationOrchestrator.Create("app", "fp-zero-patch");
        orchestrator.RecordPipelineStageReached(AutonomousPipelineStages.RepairLoop);
        for (var i = 1; i <= 3; i++)
        {
            orchestrator.RecordRecoveryEfficiency(new RecoveryEfficiencyRecord(
                i,
                RecoveryRootCauseCategory.Configuration,
                "PomSyntax",
                RecoveryMechanism.DeterministicCompile,
                0,
                false,
                DateTime.UtcNow));
        }

        var report = RecoveryEfficiencyAggregator.BuildReport(orchestrator);
        report.Insight.Should().Contain("patchesApplied=0");
    }

    [Fact]
    public void FinalizeLastRecoveryOutcome_UpdatesPendingRecord()
    {
        var orchestrator = AppGenerationOrchestrator.Create("app", "fp-finalize");
        orchestrator.RecordRecoveryEfficiency(new RecoveryEfficiencyRecord(
            1,
            RecoveryRootCauseCategory.Configuration,
            "PomSyntax",
            RecoveryMechanism.DeterministicStructural,
            1,
            null,
            DateTime.UtcNow));

        orchestrator.FinalizeLastRecoveryOutcome(true);
        orchestrator.RecoveryEfficiencyRecords[0].BuildSucceededAfterRepair.Should().BeTrue();
    }
}
