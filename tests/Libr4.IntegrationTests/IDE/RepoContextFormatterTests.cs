using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RepoContextFormatterTests
{
    private readonly RepoGraphBuilder _graph = new();
    private readonly RepoContextFormatter _formatter = new();

    [Fact]
    public void FormatFile_UsesHashPathPrefix()
    {
        var formatted = _formatter.FormatFile("backend/meals/models.py", "class Meal: pass");
        formatted.Should().Be("#backend/meals/models.py\nclass Meal: pass\n\n");
    }

    [Fact]
    public void BuildRelatedContext_OrdersDjangoLayersByDependency()
    {
        var files = new[]
        {
            File("backend/meals/urls.py", "urlpatterns = []"),
            File("backend/meals/views.py", "from .models import Meal"),
            File("backend/meals/models.py", "class Meal: pass")
        };

        var context = _formatter.BuildRelatedContext(files, _graph, 16_000);

        context.IndexOf("#backend/meals/models.py").Should().BeLessThan(context.IndexOf("#backend/meals/views.py"));
        context.IndexOf("#backend/meals/views.py").Should().BeLessThan(context.IndexOf("#backend/meals/urls.py"));
    }

    [Fact]
    public void BuildRelatedContext_EvictsDependentsFirstWhenBudgetTight()
    {
        var files = new[]
        {
            File("backend/meals/urls.py", new string('u', 400)),
            File("backend/meals/views.py", new string('v', 400)),
            File("backend/meals/models.py", new string('m', 400))
        };

        var context = _formatter.BuildRelatedContext(files, _graph, 900);

        context.Should().Contain("#backend/meals/models.py");
        context.Should().NotContain("#backend/meals/urls.py");
    }

    [Fact]
    public async Task ContextPackBuilder_IncludesRelatedFilesSection()
    {
        var memory = new InMemoryMemoryStore();
        var options = Options.Create(new ContextPackOptions
        {
            UseRepoGraphOrdering = true,
            GenerationMaxChars = 16_000
        });
        var builder = new ContextPackBuilder(memory, options, _formatter, _graph);
        var orchestrator = AppGenerationOrchestrator.Create("calorie app", "fp-test");
        orchestrator.AttachPlan(new GenerationPlan(
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
            buildCommands: new[] { "cd backend && pip install -r requirements.txt" },
            testCommands: Array.Empty<string>()));
        orchestrator.UpsertFile(File("backend/meals/models.py", "class Meal: pass"));
        orchestrator.UpsertFile(File("backend/meals/views.py", "def list_meals(): pass"));

        var pack = await builder.BuildPackAsync("post_generation", orchestrator, 16_000);

        pack.Should().Contain("## related_files");
        pack.Should().Contain("#backend/meals/models.py");
        pack.IndexOf("#backend/meals/models.py").Should().BeLessThan(pack.IndexOf("#backend/meals/views.py"));
    }

    private static GeneratedFile File(string path, string content) =>
        new(path, "python", content);
}
