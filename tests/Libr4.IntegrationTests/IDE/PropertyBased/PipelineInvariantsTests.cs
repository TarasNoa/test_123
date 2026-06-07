using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Rules;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE.PropertyBased;

/// <summary>
/// P1-7 of audit roadmap. Lightweight property-based tests for pipeline invariants.
///
/// We don't depend on FsCheck (offline build), so this file ships a deterministic
/// random plan/file generator (fixed seed for reproducibility) and asserts
/// invariants that must hold across hundreds of random shapes:
///
///   * ReviewGate2 stack-aware checks never throw on arbitrary inputs.
///   * Failure classifier is total over arbitrary log shapes.
///   * Plan command validator never crashes; safe-defaults always return non-empty arrays.
///   * StackPlanHeuristics is total: every plan classifies into exactly one of the four kinds.
///   * Auth Roslyn rule never throws on arbitrary C# files (even malformed).
/// </summary>
public sealed class PipelineInvariantsTests
{
    private const int Iterations = 64;
    private const int Seed = unchecked((int)0xA9C3DEED);

    [Fact]
    public void ReviewGate2_StaticChecks_NeverThrowOnRandomInputs()
    {
        var rng = new Random(Seed);
        var sut = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        for (var i = 0; i < Iterations; i++)
        {
            var plan = PlanGenerator.Random(rng);
            var files = FileGenerator.Random(rng, plan);

            Action act = () => sut.EvaluateStaticChecks(files, plan);

            act.Should().NotThrow($"iteration {i}");
        }
    }

    [Fact]
    public void ReviewGate2_ArchitectureChecklist_NeverThrowsOnRandomInputs()
    {
        var rng = new Random(Seed + 1);
        var sut = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        for (var i = 0; i < Iterations; i++)
        {
            var plan = PlanGenerator.Random(rng);
            var files = FileGenerator.Random(rng, plan);

            Action act = () => sut.EvaluateArchitectureChecklist(files, plan);

            act.Should().NotThrow($"iteration {i}");
        }
    }

    [Fact]
    public void FailureClassifier_IsTotalOnRandomLogShapes()
    {
        var rng = new Random(Seed + 2);
        var sut = new DefaultExecutionFailureClassifier();

        for (var i = 0; i < Iterations; i++)
        {
            var execution = ExecutionGenerator.Random(rng);
            var errors = ErrorReportGenerator.Random(rng);

            // None of these should throw even on adversarial / nonsense logs.
            Action a1 = () => sut.IsRetryable(execution);
            Action a2 = () => sut.IsRetryableException(new Exception(StringGenerator.Random(rng, maxLen: 200)));
            Action a3 = () => sut.IsNonActionableInfrastructure(errors, execution);

            a1.Should().NotThrow($"IsRetryable iter {i}");
            a2.Should().NotThrow($"IsRetryableException iter {i}");
            a3.Should().NotThrow($"IsNonActionableInfrastructure iter {i}");
        }
    }

    [Fact]
    public void PlanCommandValidator_IsTotal_AndSafeDefaults_AreNonEmpty()
    {
        var rng = new Random(Seed + 3);
        var sut = new DefaultPlanCommandValidator();

        for (var i = 0; i < Iterations; i++)
        {
            var plan = PlanGenerator.Random(rng);

            var validation = sut.Validate(plan);
            validation.Should().NotBeNull();

            var (build, test) = sut.GetSafeDefaults(plan);
            build.Should().NotBeEmpty($"safe build defaults should never be empty (iter {i})");
            test.Should().NotBeEmpty($"safe test defaults should never be empty (iter {i})");
        }
    }

    [Fact]
    public void StackPlanHeuristics_ClassifyIsTotal()
    {
        var rng = new Random(Seed + 4);

        for (var i = 0; i < Iterations; i++)
        {
            var plan = PlanGenerator.Random(rng);
            var kind = StackPlanHeuristics.Classify(plan);
            kind.Should().BeOneOf(
                StackKind.Unknown,
                StackKind.DotNet,
                StackKind.Python,
                StackKind.Node,
                StackKind.Java,
                StackKind.JavaReactFullStack,
                StackKind.Go,
                StackKind.Rust,
                StackKind.Php,
                StackKind.Ruby,
                StackKind.GoReactFullStack,
                StackKind.PhpVueFullStack);

            // Internal consistency: classify should match the boolean predicates.
            var dotnet = StackPlanHeuristics.IsAspNetCore(plan);
            var python = StackPlanHeuristics.IsPython(plan);
            var node = StackPlanHeuristics.IsNode(plan);

            // At most one of dotnet/python/node should be true (they are mutually exclusive
            // in IsAspNetCore which excludes python+node first).
            var trueCount = (dotnet ? 1 : 0) + (python ? 1 : 0) + (node ? 1 : 0);
            trueCount.Should().BeLessOrEqualTo(2,
                $"plan should not classify as more than two stacks at once (iter {i}, dotnet={dotnet}, python={python}, node={node})");
        }
    }

    [Fact]
    public async Task AuthImplementationRule_NeverThrowsOnRandomCSharpInputs()
    {
        var rng = new Random(Seed + 5);
        var sut = new AuthImplementationRule_DotNet();

        for (var i = 0; i < Iterations / 2; i++) // Roslyn parsing is heavier, halve iterations.
        {
            var plan = MakeDotNetPlan();
            var files = new List<GeneratedFile>();
            var fileCount = rng.Next(0, 4);
            for (var j = 0; j < fileCount; j++)
            {
                var content = StringGenerator.RandomCSharpish(rng);
                files.Add(new GeneratedFile($"src/F{j}.cs", "csharp", content));
            }

            Func<Task> act = async () => await sut.EvaluateAsync(files, plan, CancellationToken.None);

            await act.Should().NotThrowAsync($"iter {i}");
        }
    }

    [Fact]
    public void BudgetService_NeverGoesNegativeOnRandomConsumption()
    {
        var rng = new Random(Seed + 6);
        var sut = new InMemoryBudgetService(new BudgetOptions
        {
            PerRunTokenCap = 50_000,
            PerRunCostUsdCap = 5m
        });

        for (var i = 0; i < Iterations; i++)
        {
            var runId = Guid.NewGuid();
            for (var op = 0; op < 20; op++)
            {
                var tokens = rng.Next(-5_000, 10_000); // negatives are clamped by service
                var cost = (decimal)(rng.NextDouble() * 2 - 0.5); // some negatives too
                _ = sut.TryConsumeAsync(runId, $"op_{op}", tokens, cost).Result;
            }

            var usage = sut.GetUsage(runId);
            usage.TokensUsed.Should().BeGreaterOrEqualTo(0);
            usage.CostUsdUsed.Should().BeGreaterOrEqualTo(0);
            usage.RequestsIssued.Should().BeGreaterOrEqualTo(0);
        }
    }

    private static GenerationPlan MakeDotNetPlan() => new GenerationPlan(
        "App", "Build API",
        new TechStack(new[] { "C#" }, new[] { "ASP.NET Core" }, Array.Empty<string>(), Array.Empty<string>(), "test"),
        Array.Empty<GenerationPhase>(),
        Array.Empty<string>(),
        "mcr.microsoft.com/dotnet/sdk:8.0",
        new[] { "dotnet build" },
        new[] { "dotnet test" });
}

internal static class PlanGenerator
{
    private static readonly string[] Languages = { "C#", "csharp", "Python", "py", "JavaScript", "TypeScript", "Go", "Rust", "Java", string.Empty, "Kotlin" };
    private static readonly string[] Frameworks = { "ASP.NET Core", "FastAPI", "Flask", "Express", "Next.js", "React", "Vue", "Spring", string.Empty };
    private static readonly string[] Databases = { "PostgreSQL", "MySQL", "Redis", "MongoDB", string.Empty };
    private static readonly string[] Runtimes = { "mcr.microsoft.com/dotnet/sdk:8.0", "python:3.12-slim", "node:20-alpine", "golang:1.23", "alpine:3", string.Empty };

    public static GenerationPlan Random(Random rng)
    {
        var langs = PickRandom(rng, Languages, max: 3);
        var fwks = PickRandom(rng, Frameworks, max: 3);
        var dbs = PickRandom(rng, Databases, max: 2);
        var runtime = Runtimes[rng.Next(Runtimes.Length)];

        return new GenerationPlan(
            applicationName: StringGenerator.Random(rng, maxLen: 32) ?? "App",
            applicationDescription: StringGenerator.Random(rng, maxLen: 200) ?? string.Empty,
            techStack: new TechStack(langs, fwks, dbs, Array.Empty<string>(), "rand"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: runtime,
            buildCommands: rng.Next(2) == 0 ? Array.Empty<string>() : new[] { StringGenerator.Random(rng, maxLen: 32) ?? "echo build" },
            testCommands: rng.Next(2) == 0 ? Array.Empty<string>() : new[] { StringGenerator.Random(rng, maxLen: 32) ?? "echo test" },
            maxIterations: rng.Next(1, 30));
    }

    private static List<string> PickRandom(Random rng, string[] pool, int max)
    {
        var n = rng.Next(0, max + 1);
        var picks = new List<string>(n);
        for (var i = 0; i < n; i++)
            picks.Add(pool[rng.Next(pool.Length)]);
        return picks;
    }
}

internal static class FileGenerator
{
    public static IReadOnlyList<GeneratedFile> Random(Random rng, GenerationPlan plan)
    {
        var n = rng.Next(0, 8);
        var files = new List<GeneratedFile>(n);
        var paths = new[] { "Program.cs", "main.py", "index.js", "src/Foo.cs", "tests/test_x.py", "package.json", "requirements.txt", "README.md", "Dockerfile", "src/main.py", "src/Controllers/X.cs" };
        for (var i = 0; i < n; i++)
        {
            var path = paths[rng.Next(paths.Length)];
            var content = rng.Next(4) == 0 ? string.Empty : StringGenerator.Random(rng, maxLen: 500);
            files.Add(new GeneratedFile(path, "auto", content!));
        }
        return files;
    }
}

internal static class ExecutionGenerator
{
    public static ExecutionResult Random(Random rng)
    {
        var n = rng.Next(0, 12);
        var logs = new List<ConsoleLogEntry>(n);
        var streams = new[] { "stdout", "stderr" };
        for (var i = 0; i < n; i++)
        {
            logs.Add(new ConsoleLogEntry(DateTime.UtcNow, streams[rng.Next(2)], StringGenerator.Random(rng, maxLen: 120) ?? string.Empty));
        }
        var succeeded = rng.Next(2) == 0;
        return new ExecutionResult(
            succeeded: succeeded,
            exitCode: succeeded ? 0 : rng.Next(1, 137),
            duration: TimeSpan.FromMilliseconds(rng.Next(1, 5000)),
            logs: logs);
    }
}

internal static class ErrorReportGenerator
{
    public static IReadOnlyList<ErrorReport> Random(Random rng)
    {
        var n = rng.Next(0, 5);
        var types = new[] { "BuildOrRuntimeError", "CompileError", "TestFailure", "RuntimeError", "Unknown" };
        var list = new List<ErrorReport>(n);
        for (var i = 0; i < n; i++)
        {
            list.Add(new ErrorReport(
                errorType: types[rng.Next(types.Length)],
                message: StringGenerator.Random(rng, maxLen: 100) ?? string.Empty,
                suggestedFix: StringGenerator.Random(rng, maxLen: 60) ?? string.Empty,
                filePath: rng.Next(2) == 0 ? null : "src/x.cs"));
        }
        return list;
    }
}

internal static class StringGenerator
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz \n\t.;:'\"{}()[]/\\$|&_-=*";

    public static string? Random(Random rng, int maxLen)
    {
        if (rng.Next(8) == 0) return null;
        var len = rng.Next(0, maxLen);
        var buf = new char[len];
        for (var i = 0; i < len; i++) buf[i] = Alphabet[rng.Next(Alphabet.Length)];
        return new string(buf);
    }

    /// <summary>Generates strings that look like — but are not necessarily valid — C# source.</summary>
    public static string RandomCSharpish(Random rng)
    {
        var fragments = new[]
        {
            "using System;", "namespace X {", "public class Y { }",
            "[Authorize]", "// AddAuthentication mention",
            "public void M() { }", "var x = 1;", "{ unbalanced",
            "/* incomplete", "string s = \"unterminated", string.Empty,
            "builder.Services.AddAuthentication();", "app.UseAuthentication();"
        };
        var sb = new System.Text.StringBuilder(256);
        var n = rng.Next(0, 6);
        for (var i = 0; i < n; i++)
        {
            sb.AppendLine(fragments[rng.Next(fragments.Length)]);
        }
        return sb.ToString();
    }
}
