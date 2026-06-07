using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Fragments;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ContextFragmentManagerTests
{
    private static ContextFragmentManager CreateManager(int maxTotal = 500) =>
        new(Options.Create(new ContextFragmentOptions
        {
            MaxTotalChars = maxTotal,
            PerTypeCaps = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["build_log"] = 300,
                ["error_report"] = 200,
                ["file_excerpt"] = 200,
                ["design_artifact"] = 150,
                ["verify_evidence"] = 150
            }
        }));

    [Fact]
    public void Assemble_IncludesProvenanceMarkers()
    {
        var manager = CreateManager(2000);
        manager.Add(new ContextFragment(
            ContextFragmentType.BuildLog,
            "pip install failed",
            90,
            new Dictionary<string, string> { ["attempt"] = "3" }));

        var output = manager.Assemble();
        output.Should().Contain("[fragment:build_log:attempt=3]");
        output.Should().Contain("pip install failed");
    }

    [Fact]
    public void Assemble_EvictsLowestPriority_WhenOverCap()
    {
        var manager = CreateManager(220);
        manager.Add(new ContextFragment(ContextFragmentType.DesignArtifact, new string('d', 120), 60));
        manager.Add(new ContextFragment(ContextFragmentType.ErrorReport, new string('e', 120), 100));

        var output = manager.Assemble();
        output.Should().Contain("[fragment:error_report");
        output.Should().NotContain("[fragment:design_artifact");
    }

    [Fact]
    public void RepairAssembler_BuildsAllFragmentTypes()
    {
        var manager = new ContextFragmentManager(Options.Create(new ContextFragmentOptions
        {
            MaxTotalChars = 10_000,
            PerTypeCaps = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["build_log"] = 2_000,
                ["error_report"] = 2_000,
                ["file_excerpt"] = 4_000,
                ["design_artifact"] = 1_000,
                ["verify_evidence"] = 1_000
            }
        }));
        var files = new[]
        {
            new GeneratedFile(
                "backend/views.py",
                "python",
                string.Join('\n', Enumerable.Range(1, 80).Select(i => $"line {i}")))
        };
        var errors = new[]
        {
            new ErrorReport("SyntaxError", "invalid syntax", "fix indentation", "backend/views.py", 42)
        };

        ContextFragmentRepairAssembler.Populate(manager, new RepairFragmentInput(
            BuildLog: "ModuleNotFoundError: django\npytest failed",
            Errors: errors,
            WorkingFiles: files,
            RepairAttempt: 3,
            DesignArtifactJson: """{"palette":"dark"}"""));

        var output = manager.Assemble();
        output.Should().Contain("[fragment:build_log:attempt=3]");
        output.Should().Contain("[fragment:error_report:");
        output.Should().Contain("[fragment:file_excerpt:");
        output.Should().Contain("path=backend/views.py");
        output.Should().Contain("  42|");
        output.Should().Contain("[fragment:verify_evidence:");
        output.Should().Contain("[fragment:design_artifact:");
    }

    [Fact]
    public void BuildUserObjective_UsesFragments_InsteadOfRawBuildLog()
    {
        var registry = new AgentToolRegistry(Array.Empty<Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions.IAgentTool>());
        var plan = new GenerationPlan(
            applicationName: "CalorieVision",
            applicationDescription: "Calorie tracker",
            techStack: new TechStack(
                new[] { "Python" },
                new[] { "Django" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "django"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.12-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>());

        var fragments = "[fragment:build_log:attempt=2]\npytest FAILED";
        var objective = AgentPromptBuilder.BuildUserObjective(
            "Fix tests",
            plan,
            buildLog: new string('x', 20_000),
            registry,
            contextFragments: fragments);

        objective.Should().Contain("CONTEXT FRAGMENTS");
        objective.Should().Contain("[fragment:build_log:attempt=2]");
        objective.Should().NotContain(new string('x', 1000));
    }
}
