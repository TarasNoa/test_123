using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.StackStrategy;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class StackStrategyResolverTests
{
    private readonly IStackStrategyResolver _sut = new StackStrategyResolver(new IStackStrategy[]
    {
        new DotNetStackStrategy(),
        new PythonStackStrategy(),
        new NodeStackStrategy(),
        new UnknownStackStrategy()
    });

    [Fact]
    public void Resolve_PythonPlan_ReturnsPython()
    {
        var plan = MakePlan(new[] { "python" }, new[] { "fastapi" }, "python:3.12");

        var s = _sut.Resolve(plan);

        s.StackId.Should().Be("python");
        s.Kind.Should().Be(StackKind.Python);
        s.DefaultBuildCommands.Should().Contain(c => c.Contains("pip install -r requirements.txt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolve_NodePlan_ReturnsNode()
    {
        var plan = MakePlan(new[] { "javascript" }, new[] { "express" }, "node:20");

        var s = _sut.Resolve(plan);

        s.StackId.Should().Be("node");
        s.PreferredRuntimeImage.Should().Be("node:20-alpine");
    }

    [Fact]
    public void Resolve_DotNetPlan_ReturnsDotNet()
    {
        var plan = MakePlan(new[] { "C#" }, new[] { "ASP.NET Core" }, "mcr.microsoft.com/dotnet/sdk:8.0", "Build API");

        var s = _sut.Resolve(plan);

        s.StackId.Should().Be("dotnet");
        s.DefaultTestCommands.Should().Contain(c => c.Contains("dotnet test"));
    }

    [Fact]
    public void Resolve_UnknownPlan_FallsBackToUnknown()
    {
        var plan = MakePlan(new[] { "haskell" }, new[] { "yesod" }, "haskell:9");

        var s = _sut.Resolve(plan);

        s.StackId.Should().Be("unknown");
        s.Kind.Should().Be(StackKind.Unknown);
    }

    [Fact]
    public void IsTestPath_PythonStrategy_AcceptsPytestStyle()
    {
        var s = new PythonStackStrategy();
        s.IsTestPath("tests/test_main.py").Should().BeTrue();
        s.IsTestPath("src/main.py").Should().BeFalse();
    }

    [Fact]
    public void IsTestPath_NodeStrategy_AcceptsJestStyle()
    {
        var s = new NodeStackStrategy();
        s.IsTestPath("__tests__/foo.test.js").Should().BeTrue();
        s.IsTestPath("src/index.js").Should().BeFalse();
    }

    [Fact]
    public void All_IncludesAllRegisteredStrategies()
    {
        _sut.All.Should().HaveCount(4);
        _sut.All.Select(s => s.StackId).Should().BeEquivalentTo(new[] { "python", "node", "dotnet", "unknown" });
    }

    [Fact]
    public void Strategies_AreMutuallyExclusiveOnIdiomaticPlans()
    {
        var pythonPlan = MakePlan(new[] { "python" }, new[] { "fastapi" }, "python:3.12");
        var nodePlan = MakePlan(new[] { "javascript" }, new[] { "express" }, "node:20");
        var dotnetPlan = MakePlan(new[] { "C#" }, new[] { "ASP.NET Core" }, "mcr.microsoft.com/dotnet/sdk:8.0", "API");

        _sut.Resolve(pythonPlan).StackId.Should().Be("python");
        _sut.Resolve(nodePlan).StackId.Should().Be("node");
        _sut.Resolve(dotnetPlan).StackId.Should().Be("dotnet");
    }

    private static GenerationPlan MakePlan(IReadOnlyList<string> langs, IReadOnlyList<string> fwks, string runtime, string desc = "Test app")
        => new GenerationPlan(
            applicationName: "App",
            applicationDescription: desc,
            techStack: new TechStack(langs, fwks, Array.Empty<string>(), Array.Empty<string>(), "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: runtime,
            buildCommands: new[] { "echo build" },
            testCommands: new[] { "echo test" });
}
