using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class UpstreamSemanticAdaptationEnricherTests
{
    [Fact]
    public void Apply_ExtractsTypesAndColumns_FromUpstreamSnapshot()
    {
        var plan = new GenerationPlan(
            "GeneratedApp",
            "[[REPO_BOOTSTRAP_REQUIRED]]",
            new TechStack(new[] { "C#" }, new[] { "ASP.NET Core" }, Array.Empty<string>(), Array.Empty<string>(), "t"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            Array.Empty<string>(),
            3);

        var files = new List<GeneratedFile>
        {
            new("src/GeneratedApp.Api/GeneratedApp.Api.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />"),
            new("src/GeneratedApp.Api/Services/KanbanBoardService.cs", "csharp", "namespace X; public class KanbanBoardService {}"),
            new("upstream/src/board.ts", "typescript",
                """
                export enum ColumnStatus { Backlog = 'backlog', Done = 'done' }
                export interface Card { id: string; title: string; }
                const boardColumns = ['Backlog', 'Review', 'Done'];
                """),
            new("upstream/README.md", "markdown", "kanban board with columns and cards")
        };

        var changed = UpstreamSemanticAdaptationEnricher.Apply(plan, files);

        changed.Should().BeGreaterThan(0);
        files.Should().Contain(f => f.RelativePath == "UPSTREAM_SEMANTIC_EXTRACT.md");
        files.Should().Contain(f => f.RelativePath == "src/GeneratedApp.Api/Domain/UpstreamSemanticMap.cs");
        files.Should().Contain(f => f.RelativePath == "src/GeneratedApp.Api/Domain/UpstreamAdaptedTypes.cs");
        files.Single(f => f.RelativePath == "src/GeneratedApp.Api/Domain/UpstreamSemanticMap.cs")
            .Content.Should().Contain("ColumnStatus");
        files.Single(f => f.RelativePath == "src/GeneratedApp.Api/Domain/UpstreamAdaptedTypes.cs")
            .Content.Should().Contain("enum ColumnStatus");
        files.Single(f => f.RelativePath == "src/GeneratedApp.Api/Services/KanbanBoardService.cs")
            .Content.Should().Contain("UpstreamSemanticMap");
    }
}
