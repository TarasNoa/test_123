using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.AutonomousAppGeneration.Agents;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RepoGraphBuilderTests
{
    private readonly RepoGraphBuilder _builder = new();

    [Fact]
    public void OrderForGeneration_DjangoApp_PutsModelsBeforeViewsBeforeUrls()
    {
        var paths = new[]
        {
            "backend/meals/urls.py",
            "backend/meals/views.py",
            "backend/meals/models.py",
            "backend/meals/serializers.py"
        };

        var ordered = _builder.OrderForGeneration(paths).ToList();

        ordered.IndexOf("backend/meals/models.py").Should().BeLessThan(ordered.IndexOf("backend/meals/serializers.py"));
        ordered.IndexOf("backend/meals/serializers.py").Should().BeLessThan(ordered.IndexOf("backend/meals/views.py"));
        ordered.IndexOf("backend/meals/views.py").Should().BeLessThan(ordered.IndexOf("backend/meals/urls.py"));
    }

    [Fact]
    public void OrderForRepair_ReversesGenerationOrder()
    {
        var paths = new[]
        {
            "backend/meals/urls.py",
            "backend/meals/views.py",
            "backend/meals/models.py"
        };

        var generation = _builder.OrderForGeneration(paths);
        var repair = _builder.OrderForRepair(paths);

        repair.Should().Equal(generation.Reverse());
    }

    [Fact]
    public void OrderBatches_SortsWithinFeatureBatch()
    {
        var entries = new[]
        {
            new PlannedFileEntry("backend/meals/urls.py", AgentPhase.Backend, "urls", "python"),
            new PlannedFileEntry("backend/meals/views.py", AgentPhase.Backend, "views", "python"),
            new PlannedFileEntry("backend/meals/models.py", AgentPhase.Backend, "models", "python")
        };
        var batches = new List<IReadOnlyList<PlannedFileEntry>>
        {
            entries
        };

        var ordered = RepoGraphBatchOrdering.OrderBatches(batches, _builder);
        ordered[0].Select(e => e.Path).Should().Equal(
            "backend/meals/models.py",
            "backend/meals/views.py",
            "backend/meals/urls.py");
    }
}
