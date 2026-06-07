using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.BatchCi;

public sealed record BenchmarkRegressionScenario(
    string Id,
    string DisplayName,
    string UserRequest,
    string ExpectedRecipeId);

public static class BenchmarkRegressionCatalog
{
    public static IReadOnlyList<BenchmarkRegressionScenario> NightlyScenarios { get; } =
    [
        new(
            "calorie-vision",
            "CalorieVision (Django + SolidJS)",
            "Build CalorieVision: Django REST backend with calorie tracking API and SolidJS frontend with dashboard.",
            "calorie-vision"),
        new(
            "banking",
            "Banking (Spring Boot + React)",
            "Build a Banking app: Spring Boot backend with accounts/transfers and React frontend.",
            "banking"),
        new(
            "nextjs",
            "Next.js Fullstack",
            "Build a Next.js 14 fullstack todo app with API routes and Tailwind UI.",
            "nextjs")
    ];
}

public sealed record BenchmarkKpiGateResult(
    string Gate,
    bool Passed,
    string Detail);

public sealed record BenchmarkRegressionEvaluation(
    string ScenarioId,
    Guid? RunId,
    bool Passed,
    IReadOnlyList<BenchmarkKpiGateResult> Gates,
    string? PipelineStageReached,
    int PatchesApplied);

public sealed record BenchmarkRegressionHarnessReport(
    DateTime EvaluatedAtUtc,
    bool AllPassed,
    IReadOnlyList<BenchmarkRegressionEvaluation> Scenarios);

public interface IBenchmarkRegressionHarness
{
    BenchmarkRegressionEvaluation EvaluateScenario(
        BenchmarkRegressionScenario scenario,
        AppGenerationOrchestrator orchestrator);

    BenchmarkRegressionHarnessReport EvaluateAll(IReadOnlyList<AppGenerationOrchestrator> runsByScenarioOrder);

    IReadOnlyList<BenchmarkRegressionScenario> GetNightlyScenarios();
}

public sealed class BenchmarkRegressionHarness : IBenchmarkRegressionHarness
{
    public IReadOnlyList<BenchmarkRegressionScenario> GetNightlyScenarios() =>
        BenchmarkRegressionCatalog.NightlyScenarios;

    public BenchmarkRegressionHarnessReport EvaluateAll(IReadOnlyList<AppGenerationOrchestrator> runsByScenarioOrder)
    {
        var scenarios = BenchmarkRegressionCatalog.NightlyScenarios;
        var evaluations = new List<BenchmarkRegressionEvaluation>();
        for (var i = 0; i < scenarios.Count; i++)
        {
            var orchestrator = i < runsByScenarioOrder.Count ? runsByScenarioOrder[i] : null;
            evaluations.Add(orchestrator is null
                ? Failed(scenarios[i], null, "run_missing", orchestrator: null)
                : EvaluateScenario(scenarios[i], orchestrator));
        }

        return new BenchmarkRegressionHarnessReport(
            DateTime.UtcNow,
            evaluations.All(e => e.Passed),
            evaluations);
    }

    public BenchmarkRegressionEvaluation EvaluateScenario(
        BenchmarkRegressionScenario scenario,
        AppGenerationOrchestrator orchestrator)
    {
        var stage = orchestrator.PipelineStageReached;
        var patchesApplied = orchestrator.RecoveryEfficiencyRecords.Sum(r => r.PatchesApplied);
        var gates = new List<BenchmarkKpiGateResult>
        {
            EvaluateStageGate(stage),
            EvaluatePatchesGate(patchesApplied, stage)
        };

        var passed = gates.All(g => g.Passed);
        return new BenchmarkRegressionEvaluation(
            scenario.Id,
            orchestrator.Id,
            passed,
            gates,
            stage,
            patchesApplied);
    }

    public static BenchmarkKpiGateResult EvaluateStageGate(string? stage)
    {
        var order = AutonomousPipelineStages.GetOrder(stage ?? string.Empty);
        var repairOrder = AutonomousPipelineStages.GetOrder(AutonomousPipelineStages.RepairLoop);
        var passed = order >= repairOrder;
        return new BenchmarkKpiGateResult(
            "PipelineStageReached>=RepairLoop",
            passed,
            passed
                ? $"reached={stage}"
                : $"reached={stage ?? "Unknown"}, required>={AutonomousPipelineStages.RepairLoop}");
    }

    public static BenchmarkKpiGateResult EvaluatePatchesGate(int patchesApplied, string? stage)
    {
        if (!AutonomousPipelineStages.IsRecoveryMeasurementEligible(stage))
        {
            return new BenchmarkKpiGateResult(
                "patchesApplied>0",
                false,
                $"recovery_not_eligible stage={stage ?? "Unknown"}");
        }

        var passed = patchesApplied > 0;
        return new BenchmarkKpiGateResult(
            "patchesApplied>0",
            passed,
            passed ? $"patches={patchesApplied}" : "no repair patches recorded");
    }

    private static BenchmarkRegressionEvaluation Failed(
        BenchmarkRegressionScenario scenario,
        Guid? runId,
        string reason,
        AppGenerationOrchestrator? orchestrator) =>
        new(
            scenario.Id,
            runId,
            false,
            [
                new BenchmarkKpiGateResult("run_present", false, reason),
                EvaluateStageGate(orchestrator?.PipelineStageReached),
                EvaluatePatchesGate(
                    orchestrator?.RecoveryEfficiencyRecords.Sum(r => r.PatchesApplied) ?? 0,
                    orchestrator?.PipelineStageReached)
            ],
            orchestrator?.PipelineStageReached,
            orchestrator?.RecoveryEfficiencyRecords.Sum(r => r.PatchesApplied) ?? 0);
}
