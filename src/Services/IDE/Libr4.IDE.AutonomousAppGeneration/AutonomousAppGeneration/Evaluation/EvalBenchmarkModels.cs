namespace Libr4.IDE.Application.AutonomousAppGeneration.Evaluation;

public sealed class EvalBenchmarkTestCase
{
    public string Name { get; set; } = string.Empty;
    public List<string> RequiredPatterns { get; set; } = [];
    public List<string> AnyOf { get; set; } = [];
}

public sealed class EvalBenchmarkDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Stack { get; set; } = string.Empty;
    public string Style { get; set; } = EvalBenchmarkStyles.HumanEval;
    public string? Language { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public List<string> RequiredPatterns { get; set; } = [];
    public List<string> ForbiddenPatterns { get; set; } = [];
    public List<EvalBenchmarkTestCase> Tests { get; set; } = [];
}

public sealed record EvalCaseResult(
    string BenchmarkId,
    string Stack,
    string Style,
    bool Passed,
    IReadOnlyList<string> Failures);

public sealed record EvalStackScore(string Stack, int Total, int Passed, double PassRate);

public sealed record EvalSuiteReport(
    DateTime EvaluatedAtUtc,
    int Total,
    int Passed,
    double OverallPassRate,
    IReadOnlyList<EvalStackScore> StackScores,
    IReadOnlyList<EvalCaseResult> Cases);

public sealed record EvalBaselineScores(
    int Version,
    double Overall,
    Dictionary<string, double> Stacks);

public sealed record EvalRegressionGateResult(
    bool Passed,
    double CurrentOverall,
    double BaselineOverall,
    IReadOnlyList<(string Stack, double Current, double Baseline, bool Passed)> StackChecks,
    string? FailureReason);

public interface IInternalEvalHarness
{
    IReadOnlyList<EvalBenchmarkDefinition> LoadBenchmarks();

    EvalCaseResult EvaluateBenchmark(EvalBenchmarkDefinition benchmark, string candidate);

    EvalSuiteReport RunSuite(IReadOnlyDictionary<string, string>? candidatesById = null);

    EvalRegressionGateResult CheckRegressionGate(EvalSuiteReport report);
}
