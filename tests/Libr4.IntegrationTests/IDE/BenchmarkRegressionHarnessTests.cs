using FluentAssertions;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.BatchCi;
using Libr4.IDE.Application.AutonomousAppGeneration.HostProfiles;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class BenchmarkRegressionHarnessTests
{
    private readonly BenchmarkRegressionHarness _harness = new();

    [Fact]
    public void EvaluateStageGate_RepairLoop_Passes()
    {
        var gate = BenchmarkRegressionHarness.EvaluateStageGate(AutonomousPipelineStages.RepairLoop);
        gate.Passed.Should().BeTrue();
    }

    [Fact]
    public void EvaluateStageGate_Planning_Fails()
    {
        var gate = BenchmarkRegressionHarness.EvaluateStageGate(AutonomousPipelineStages.Planning);
        gate.Passed.Should().BeFalse();
    }

    [Fact]
    public void EvaluatePatchesGate_AfterStartupBuild_RequiresPatches()
    {
        BenchmarkRegressionHarness.EvaluatePatchesGate(0, AutonomousPipelineStages.RepairLoop)
            .Passed.Should().BeFalse();
        BenchmarkRegressionHarness.EvaluatePatchesGate(2, AutonomousPipelineStages.RepairLoop)
            .Passed.Should().BeTrue();
    }

    [Fact]
    public void EvaluateScenario_PassingRun_MeetsKpiGates()
    {
        var orchestrator = AppGenerationOrchestrator.Create("build app", "fp");
        orchestrator.RecordPipelineStageReached(AutonomousPipelineStages.RepairLoop);
        orchestrator.RecordRecoveryEfficiency(new RecoveryEfficiencyRecord(
            IterationNumber: 1,
            RootCauseCategory: RecoveryRootCauseCategory.Dependencies,
            PrimaryErrorClass: "CS1002",
            Mechanism: RecoveryMechanism.Llm,
            PatchesApplied: 3,
            BuildSucceededAfterRepair: null,
            AttemptedAtUtc: DateTime.UtcNow,
            ErrorSignature: "cs1002",
            RepairDurationMs: 1200));

        var scenario = BenchmarkRegressionCatalog.NightlyScenarios[0];
        var result = _harness.EvaluateScenario(scenario, orchestrator);
        result.Passed.Should().BeTrue();
        result.Gates.Should().Contain(g => g.Gate == "PipelineStageReached>=RepairLoop" && g.Passed);
        result.Gates.Should().Contain(g => g.Gate == "patchesApplied>0" && g.Passed);
    }

    [Fact]
    public void NightlyCatalog_ContainsCalorieVisionBankingNextJs()
    {
        var ids = BenchmarkRegressionCatalog.NightlyScenarios.Select(s => s.Id).ToArray();
        ids.Should().Contain("calorie-vision");
        ids.Should().Contain("banking");
        ids.Should().Contain("nextjs");
    }

    [Fact]
    public void BatchLlmProfileScope_ActivatesForCiTrigger()
    {
        var scope = new AutonomousBatchLlmProfileScope(
            Options.Create(new AutonomousBatchLlmProfileOptions
            {
                UseBatchLlmProfile = false,
                Model = "openai/gpt-4o-mini",
                DisableStreaming = true
            }),
            Options.Create(new AutonomousHostProfileOptions
            {
                ActiveProfile = AutonomousHostProfile.DockerModelRunner
            }),
            NullLogger<AutonomousBatchLlmProfileScope>.Instance);

        scope.ShouldUseBatchProfile("nightly-ci").Should().BeTrue();
        scope.ShouldUseBatchProfile("manual").Should().BeFalse();
    }

    [Fact]
    public void LlmCallPreferenceContext_OverridesModelWhileActive()
    {
        LlmCallPreferenceContext.CurrentPreferences.Should().BeNull();
        using (LlmCallPreferenceContext.Activate(new LlmCallPreferences("batch/model", DisableStreaming: true)))
        {
            LlmCallPreferenceContext.CurrentPreferences!.ModelOverride.Should().Be("batch/model");
            LlmCallPreferenceContext.CurrentPreferences!.DisableStreaming.Should().BeTrue();
        }

        LlmCallPreferenceContext.CurrentPreferences.Should().BeNull();
    }
}
