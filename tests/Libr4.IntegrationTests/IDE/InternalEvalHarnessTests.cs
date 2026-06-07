using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Evaluation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class InternalEvalHarnessTests
{
    private readonly InternalEvalHarness _harness;

    public InternalEvalHarnessTests()
    {
        var evaluationRoot = ResolveEvaluationRoot();
        var options = Options.Create(new InternalEvalOptions
        {
            Enabled = true,
            EvaluationRoot = evaluationRoot
        });
        var catalog = new EvalBenchmarkCatalog(options);
        _harness = new InternalEvalHarness(catalog, options, NullLogger<InternalEvalHarness>.Instance);
    }

    [Fact]
    public void Catalog_LoadsAllStackBenchmarks()
    {
        var benchmarks = _harness.LoadBenchmarks();
        benchmarks.Should().NotBeEmpty();
        benchmarks.Select(b => b.Stack).Distinct().Should().BeEquivalentTo(EvalStackNames.All);
    }

    [Fact]
    public void RunSuite_ReferenceSolutions_PassAllHumanEvalAndMbppCases()
    {
        var report = _harness.RunSuite();
        report.Total.Should().BeGreaterThanOrEqualTo(8);
        report.Passed.Should().Be(report.Total);
        report.OverallPassRate.Should().Be(1.0);
        report.StackScores.Should().OnlyContain(s => s.PassRate == 1.0);
    }

    [Fact]
    public void EvaluateBenchmark_BadCandidate_FailsHumanEval()
    {
        var benchmark = _harness.LoadBenchmarks().First(b => b.Id == "django-health-view");
        var result = _harness.EvaluateBenchmark(benchmark, "def health(): pass");
        result.Passed.Should().BeFalse();
        result.Failures.Should().NotBeEmpty();
    }

    [Fact]
    public void RegressionGate_ReferenceSuite_DoesNotRegressBaseline()
    {
        var report = _harness.RunSuite();
        var gate = _harness.CheckRegressionGate(report);
        gate.Passed.Should().BeTrue();
        gate.FailureReason.Should().BeNull();
        gate.StackChecks.Should().OnlyContain(c => c.Passed);
    }

    private static string ResolveEvaluationRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Evaluation");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Evaluation root not found for tests");
    }
}
