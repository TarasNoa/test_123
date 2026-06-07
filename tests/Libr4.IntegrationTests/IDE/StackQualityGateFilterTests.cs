using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class StackQualityGateFilterTests
{
    [Fact]
    public void Apply_DotNetFilter_KeepsUniversalAndDotNetGates()
    {
        var timeline = SampleTimeline();
        var filtered = StackQualityGateFilter.Apply(timeline, "dotnet");

        filtered.Should().Contain(g => g.Stage.Contains("verify_subagent", StringComparison.OrdinalIgnoreCase));
        filtered.Should().Contain(g => g.Stage.Contains("build:stack_safety_net", StringComparison.OrdinalIgnoreCase));
        filtered.Should().NotContain(g => g.Stage.Contains("maven_failed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Apply_BankingFilter_IncludesMavenGates()
    {
        var timeline = SampleTimeline();
        var filtered = StackQualityGateFilter.Apply(timeline, "banking");

        filtered.Should().Contain(g => g.Reasons.Any(r => r.Contains("maven", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void BuildOptions_IncludesDetectedRecipe()
    {
        var timeline = SampleTimeline();
        var detection = new VerifyRecipeDetectionResult(
            VerifyRecipeCatalog.BuildAll().Single(r => r.Id == "dotnet"),
            "deterministic");

        var options = StackQualityGateFilter.BuildOptions(timeline, detection);

        options.Should().Contain(o => o.RecipeId == "all");
        options.Should().Contain(o => o.RecipeId == "dotnet");
    }

    [Fact]
    public void Build_WithStackFilter_ReducesTimeline()
    {
        var orchestrator = AppGenerationOrchestrator.Create("Dotnet app", "fp-dotnet-filter");
        orchestrator.RecordQualityGate("verify_subagent", 10, true, ["obscura passed"]);
        orchestrator.RecordQualityGate("build:stack_safety_net", 4, false, ["dotnet build failed"]);
        orchestrator.RecordQualityGate("build:stack_safety_net", 3, false, ["maven_failed"]);

        var evidenceStore = new FileSystemVerifyEvidenceStore(
            Options.Create(new VerifySubagentOptions { EvidenceRoot = Path.Combine(Path.GetTempPath(), $"stack-gate-{Guid.NewGuid():N}") }),
            NullLogger<FileSystemVerifyEvidenceStore>.Instance);
        var service = new BuildDiagnosticsDashboardService(evidenceStore);
        var quality = new RunQualityAssessmentService().Assess(orchestrator);
        var recipe = new VerifyRecipeDetectionResult(
            VerifyRecipeCatalog.BuildAll().Single(r => r.Id == "dotnet"),
            "deterministic");

        var all = service.Build(orchestrator, quality, recipe, "all");
        var dotnetOnly = service.Build(orchestrator, quality, recipe, "dotnet");

        all.Timeline.Should().HaveCount(3);
        dotnetOnly.Timeline.Should().HaveCount(2);
        dotnetOnly.ActiveStackFilter.Should().Be("dotnet");
        dotnetOnly.VerifyRecipe!.RecipeId.Should().Be("dotnet");
        dotnetOnly.StackFilters.Should().NotBeNullOrEmpty();
    }

    private static IReadOnlyList<BuildGateTimelineEntryDto> SampleTimeline() =>
    [
        new(1, "verify_subagent", "review", "L4 business", 10, true, ["obscura passed"], DateTime.UtcNow),
        new(2, "pre_safety_normalization", "normalization", "L0 structural", 9, true, ["jwt:ok"], DateTime.UtcNow),
        new(3, "build:stack_safety_net", "build", "L1 build", 4, false, ["dotnet build failed"], DateTime.UtcNow),
        new(4, "build:stack_safety_net", "build", "L1 build", 3, false, ["maven_failed"], DateTime.UtcNow),
    ];
}
