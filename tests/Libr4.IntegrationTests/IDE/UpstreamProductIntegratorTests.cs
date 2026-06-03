using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class UpstreamProductIntegratorTests
{
    [Fact]
    public void ApplyDotNetIntegration_WritesDomainServiceAndIntegrationNotes()
    {
        var plan = new GenerationPlan(
            applicationName: "GeneratedApp",
            applicationDescription: "[[REPO_BOOTSTRAP_REQUIRED]]",
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

        const string bootstrap =
            """{"clone_url":"https://github.com/roovo/obsidian-card-board.git","repository":"roovo/obsidian-card-board","license":"MIT"}""";

        var files = new List<GeneratedFile>
        {
            new("src/GeneratedApp.Api/GeneratedApp.Api.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>"),
            new("upstream/README.md", "markdown", "Columns: \"Backlog\", \"In Progress\", \"Done\" for kanban board."),
            new("upstream/src/board.ts", "typescript", "const columns = ['Backlog', 'In Progress', 'Done'];")
        };

        var changed = UpstreamProductIntegrator.ApplyDotNetIntegration(files, plan, bootstrap);

        changed.Should().BeGreaterThan(0);
        files.Should().Contain(f => f.RelativePath == "UPSTREAM_INTEGRATION.md");
        files.Should().Contain(f => f.RelativePath == "src/GeneratedApp.Api/Domain/UpstreamKanbanBoard.cs");
        files.Should().Contain(f => f.RelativePath == "src/GeneratedApp.Api/Services/KanbanBoardService.cs");
        var controller = files.Single(f => f.RelativePath == "src/GeneratedApp.Api/Controllers/KanbanController.cs");
        controller.Content.Should().Contain("KanbanBoardService");
        controller.Content.Should().Contain("Adapted from upstream");
    }
}
