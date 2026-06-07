using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Evaluation;

public sealed class InternalEvalHarness : IInternalEvalHarness
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly EvalBenchmarkCatalog _catalog;
    private readonly InternalEvalOptions _options;
    private readonly ILogger<InternalEvalHarness> _logger;

    public InternalEvalHarness(
        EvalBenchmarkCatalog catalog,
        IOptions<InternalEvalOptions> options,
        ILogger<InternalEvalHarness> logger)
    {
        _catalog = catalog;
        _options = options.Value;
        _logger = logger;
    }

    public IReadOnlyList<EvalBenchmarkDefinition> LoadBenchmarks() => _catalog.All;

    public EvalCaseResult EvaluateBenchmark(EvalBenchmarkDefinition benchmark, string candidate)
    {
        var (passed, failures) = string.Equals(benchmark.Style, EvalBenchmarkStyles.Mbpp, StringComparison.OrdinalIgnoreCase)
            ? MbppStyleEvaluator.Evaluate(benchmark, candidate)
            : HumanEvalStyleEvaluator.Evaluate(benchmark, candidate);

        return new EvalCaseResult(
            benchmark.Id,
            benchmark.Stack,
            benchmark.Style,
            passed,
            failures);
    }

    public EvalSuiteReport RunSuite(IReadOnlyDictionary<string, string>? candidatesById = null)
    {
        if (!_options.Enabled)
        {
            return new EvalSuiteReport(
                DateTime.UtcNow,
                0,
                0,
                0,
                Array.Empty<EvalStackScore>(),
                Array.Empty<EvalCaseResult>());
        }

        var benchmarks = _catalog.All;
        var cases = new List<EvalCaseResult>();
        foreach (var benchmark in benchmarks)
        {
            var candidate = candidatesById is not null && candidatesById.TryGetValue(benchmark.Id, out var provided)
                ? provided
                : benchmark.Solution;
            cases.Add(EvaluateBenchmark(benchmark, candidate));
        }

        var stackScores = EvalStackNames.All
            .Select(stack =>
            {
                var stackCases = cases.Where(c => string.Equals(c.Stack, stack, StringComparison.OrdinalIgnoreCase)).ToList();
                var passed = stackCases.Count(c => c.Passed);
                var total = stackCases.Count;
                var rate = total == 0 ? 0 : (double)passed / total;
                return new EvalStackScore(stack, total, passed, rate);
            })
            .Where(s => s.Total > 0)
            .ToList();

        var totalCount = cases.Count;
        var passedCount = cases.Count(c => c.Passed);
        var overall = totalCount == 0 ? 0 : (double)passedCount / totalCount;

        _logger.LogInformation(
            "Internal eval suite finished: {Passed}/{Total} ({Rate:P0})",
            passedCount,
            totalCount,
            overall);

        return new EvalSuiteReport(
            DateTime.UtcNow,
            totalCount,
            passedCount,
            overall,
            stackScores,
            cases);
    }

    public EvalRegressionGateResult CheckRegressionGate(EvalSuiteReport report)
    {
        var baseline = LoadBaseline();
        var stackChecks = new List<(string Stack, double Current, double Baseline, bool Passed)>();
        foreach (var stackScore in report.StackScores)
        {
            var baselineRate = baseline.Stacks.GetValueOrDefault(stackScore.Stack, baseline.Overall);
            var passed = stackScore.PassRate + 1e-9 >= baselineRate;
            stackChecks.Add((stackScore.Stack, stackScore.PassRate, baselineRate, passed));
        }

        var overallPassed = report.OverallPassRate + 1e-9 >= baseline.Overall && stackChecks.All(s => s.Passed);
        return new EvalRegressionGateResult(
            overallPassed,
            report.OverallPassRate,
            baseline.Overall,
            stackChecks,
            overallPassed ? null : "eval_score_regressed");
    }

    private EvalBaselineScores LoadBaseline()
    {
        var path = _catalog.BaselinePath;
        if (!File.Exists(path))
        {
            _logger.LogWarning("Eval baseline not found at {Path}; using defaults", path);
            return new EvalBaselineScores(1, 1.0, EvalStackNames.All.ToDictionary(s => s, _ => 1.0));
        }

        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var version = root.TryGetProperty("version", out var versionEl) ? versionEl.GetInt32() : 1;
        var overall = root.TryGetProperty("overall", out var overallEl) ? overallEl.GetDouble() : 1.0;
        var stacks = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("stacks", out var stacksEl))
        {
            foreach (var property in stacksEl.EnumerateObject())
                stacks[property.Name] = property.Value.GetDouble();
        }

        return new EvalBaselineScores(version, overall, stacks);
    }
}
