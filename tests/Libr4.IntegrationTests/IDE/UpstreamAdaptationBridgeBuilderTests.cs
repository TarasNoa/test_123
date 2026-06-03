using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class UpstreamAdaptationBridgeBuilderTests
{
    [Fact]
    public void TryAppendBridgeDocument_AddsBridge_WhenUpstreamSnapshotPresent()
    {
        var plan = new GenerationPlan(
            applicationName: "GeneratedApp",
            applicationDescription: "repo bootstrap test",
            techStack: new TechStack(
                languages: new[] { "C#" },
                frameworks: new[] { "ASP.NET Core" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "mcr.microsoft.com/dotnet/sdk:8.0",
            buildCommands: new[] { "dotnet build" },
            testCommands: Array.Empty<string>(),
            maxIterations: 3);

        var files = new List<GeneratedFile>
        {
            new("upstream/README.md", "markdown", "# Card board\nKanban columns for Obsidian."),
            new("upstream/UPSTREAM_MANIFEST.json", "json", """{"adapted_from_upstream":true}""")
        };

        var changed = UpstreamAdaptationBridgeBuilder.TryAppendBridgeDocument(files, plan);

        changed.Should().Be(1);
        files.Should().Contain(f => f.RelativePath == "ADAPTATION_BRIDGE.md");
        var bridge = files.Single(f => f.RelativePath == "ADAPTATION_BRIDGE.md");
        bridge.Content.Should().Contain("upstream/README.md");
        bridge.Content.Should().Contain("Kanban columns");
    }
}
