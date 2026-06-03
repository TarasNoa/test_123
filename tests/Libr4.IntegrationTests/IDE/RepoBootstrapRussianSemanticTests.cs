using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RepoBootstrapRussianSemanticTests
{
  private const string RussianRepoBootstrapRequest = """
        Сгенерируй ASP.NET API: Obscura на :9222, клон GitHub (MIT), JWT, канбан-доска,
        shadow workspace, self-heal. [[REPO_BOOTSTRAP_REQUIRED]]
        """;

    [Fact]
    public void StackHeuristics_PreferAspNet_ForRussianRepoBootstrap()
    {
        StackPlanHeuristics.ShouldPreferAspNetCoreForRepoBootstrap(RussianRepoBootstrapRequest)
            .Should().BeTrue();
    }

    [Fact]
    public void Enricher_MapsRussianColumns_FromUpstreamSnapshot()
    {
        var plan = new GenerationPlan(
            "КанбанApi",
            RussianRepoBootstrapRequest,
            new TechStack(new[] { "C#" }, new[] { "ASP.NET Core" }, Array.Empty<string>(), Array.Empty<string>(), "t"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            Array.Empty<string>(),
            3);

        var files = new List<GeneratedFile>
        {
            new("src/КанбанApi/КанбанApi.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />"),
            new("src/КанбанApi/Services/KanbanBoardService.cs", "csharp", "namespace X; public class KanbanBoardService {}"),
            new("upstream/src/board.ts", "typescript",
                """
                export enum ColumnStatus { Backlog = 'backlog', Done = 'done' }
                export interface Card { id: string; title: string; }
                const boardColumns = ['Бэклог', 'В работе', 'Готово'];
                """),
            new("upstream/README.md", "markdown", "канбан доска с колонками")
        };

        UpstreamSemanticAdaptationEnricher.Apply(plan, files);

        files.Should().Contain(f => f.RelativePath.EndsWith("Domain/UpstreamAdaptedTypes.cs", StringComparison.OrdinalIgnoreCase));
        files.Should().Contain(f => f.RelativePath == "UPSTREAM_SEMANTIC_EXTRACT.md");
        files.Single(f => f.RelativePath.EndsWith("UpstreamSemanticMap.cs", StringComparison.OrdinalIgnoreCase))
            .Content.Should().Contain("MappedTypes");
    }

    [Fact]
    public void QualityGate_PassesRussianRepoBootstrap_WithSemanticArtifacts()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = true,
        }));

        var plan = new GenerationPlan(
            "КанбанAuthApi",
            RussianRepoBootstrapRequest,
            new TechStack(new[] { "C#" }, new[] { "ASP.NET Core" }, Array.Empty<string>(), Array.Empty<string>(), "bootstrap"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            new[] { "dotnet test" },
            4);

        var files = new List<GeneratedFile>
        {
            new("KanbanAuthApi.sln", "text", "x"),
            new("BOOTSTRAP_EVIDENCE.md", "markdown", "repository_url: https://github.com/example/repo license: mit адаптация upstream"),
            new("upstream/README.md", "markdown", "исходный репозиторий kanban"),
            new("UPSTREAM_SEMANTIC_EXTRACT.md", "markdown", "upstream semantic"),
            new("ADAPTATION_BRIDGE.md", "markdown", "bridge"),
            new("src/KanbanAuthApi/KanbanAuthApi.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />"),
            new("src/KanbanAuthApi/Program.cs", "csharp",
                "AddAuthentication(JwtBearerDefaults.AuthenticationScheme); UseAuthentication(); MapControllers();"),
            new("src/KanbanAuthApi/Controllers/AuthController.cs", "csharp",
                "JwtSecurityToken; [Route(\"api/auth\")] [HttpPost(\"token\")]"),
            new("src/KanbanAuthApi/Controllers/KanbanController.cs", "csharp",
                "[Authorize][Route(\"api/kanban\")] board columns tasks"),
            new("src/KanbanAuthApi/Services/KanbanService.cs", "csharp", "class KanbanService {}"),
            new("src/KanbanAuthApi/Data/AppDbContext.cs", "csharp", "class AppDbContext {}"),
            new("src/KanbanAuthApi/Domain/UpstreamAdaptedTypes.cs", "csharp", "enum ColumnStatus record Card"),
            new("tests/KanbanAuthApi.Tests/KanbanAuthApi.Tests.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk\" />"),
            new("tests/KanbanAuthApi.Tests/KanbanAuthHttpTests.cs", "csharp",
                """
                using Microsoft.AspNetCore.Mvc.Testing;
                public sealed class KanbanAuthHttpTests : IClassFixture<WebApplicationFactory<Program>>
                {
                    private readonly HttpClient _client;
                    [Fact] public async Task Token() => await _client.PostAsync("/api/auth/token", null);
                    [Fact] public async Task Board() => await _client.GetAsync("/api/kanban/board");
                }
                """),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Passed.Should().BeTrue($"reasons={string.Join(',', r.Reasons)}");
        r.Reasons.Should().NotContain("repo_bootstrap_not_reflected_in_code");
        r.Reasons.Should().NotContain("intent_kanban_not_reflected_in_code");
    }
}
