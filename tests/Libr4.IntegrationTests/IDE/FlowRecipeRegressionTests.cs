using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

/// <summary>
/// CI regression gate for versioned <c>Flows/*.flow.yaml</c> recipes.
/// </summary>
public sealed class FlowRecipeRegressionTests : IDisposable
{
    private static readonly string[] KnownRecipes =
    [
        "calorie-django-solidjs",
        "banking-java-react",
        "nextjs-shop"
    ];

    private readonly string _runsRoot;

    public FlowRecipeRegressionTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"flow-recipe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_runsRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_runsRoot))
                Directory.Delete(_runsRoot, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    [Fact]
    public void AllRecipes_LoadFromFlowsDirectory()
    {
        var docs = FlowYamlLoader.LoadDirectory(FlowsDirectory());
        docs.Select(d => d.Name).Should().BeEquivalentTo(KnownRecipes);
        docs.Should().OnlyContain(d => d.Version >= 1);
    }

    [Theory]
    [MemberData(nameof(RecipeNames))]
    public void Recipe_StructureIsValid(string flowName)
    {
        var doc = LoadRecipe(flowName);
        var def = FlowYamlLoader.ToDefinition(doc);

        def.Nodes.Should().NotBeEmpty();
        def.Nodes.Select(n => n.Id).Should().OnlyHaveUniqueItems();

        var nodeIds = def.Nodes.Select(n => n.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in def.Edges)
        {
            nodeIds.Should().Contain(edge.From);
            nodeIds.Should().Contain(edge.To);
        }

        var hasEntry = def.Nodes.Any(n =>
            def.Edges.All(e => !e.To.Equals(n.Id, StringComparison.OrdinalIgnoreCase)));
        hasEntry.Should().BeTrue($"{flowName} must have an entry node");

        var reachable = ReachableNodeIds(def);
        reachable.Should().BeEquivalentTo(nodeIds, $"{flowName} must not contain orphan nodes");
    }

    [Theory]
    [MemberData(nameof(RecipeNames))]
    public void Recipe_ResolvesSlashCommand(string flowName)
    {
        var engine = CreateEngine();
        engine.TryResolveFlowName($"/flow:{flowName} build demo", out var resolved).Should().BeTrue();
        resolved.Should().Be(flowName);
    }

    [Theory]
    [MemberData(nameof(RecipeNames))]
    public async Task Recipe_HappyPathCompletes(string flowName)
    {
        var engine = CreateEngine();
        var registry = new FlowRegistry(
            Options.Create(new FlowEngineOptions { FlowsDirectory = FlowsDirectory(), RunsRoot = _runsRoot }),
            NullLogger<FlowRegistry>.Instance);
        registry.TryGet(flowName, out var def).Should().BeTrue();

        var runId = Guid.NewGuid();
        await engine.InitializeAsync(runId, flowName);

        for (var step = 0; step < def!.Nodes.Count + 5; step++)
        {
            var progress = await engine.GetProgressAsync(runId);
            if (progress is null || progress.Status is "completed" or "failed" or "aborted")
                break;
            if (string.IsNullOrWhiteSpace(progress.CurrentNodeId))
                break;

            var node = def.Nodes.First(n => n.Id == progress.CurrentNodeId);
            var context = BuildContext(node);
            var phase = ResolvePhase(node);
            await engine.OnPhaseCompletedAsync(runId, phase, succeeded: true, context);
        }

        var final = await engine.GetProgressAsync(runId);
        final.Should().NotBeNull();
        final!.Status.Should().Be("completed", $"{flowName} happy path should finish");
    }

    public static IEnumerable<object[]> RecipeNames() =>
        KnownRecipes.Select(n => new object[] { n });

    private static string FlowsDirectory() =>
        Path.Combine(AppContext.BaseDirectory, "Flows");

    private static FlowDefinitionDocument LoadRecipe(string flowName)
    {
        var path = Path.Combine(FlowsDirectory(), $"{flowName}.flow.yaml");
        File.Exists(path).Should().BeTrue($"missing recipe file: {path}");
        return FlowYamlLoader.LoadFromFile(path);
    }

    private YamlFlowEngine CreateEngine()
    {
        var options = Options.Create(new FlowEngineOptions
        {
            FlowsDirectory = FlowsDirectory(),
            RunsRoot = _runsRoot
        });
        var registry = new FlowRegistry(options, NullLogger<FlowRegistry>.Instance);
        var store = new FileFlowProgressStore(options);
        return new YamlFlowEngine(registry, store, options);
    }

    private static string ResolvePhase(FlowNode node) =>
        !string.IsNullOrWhiteSpace(node.Stage) ? node.Stage! : node.Id;

    private static FlowRuntimeContext BuildContext(FlowNode node)
    {
        if (node.Type == FlowNodeType.Gate)
        {
            var files = node.Preconditions
                .Where(p => p.Kind.Equals("files_exist", StringComparison.OrdinalIgnoreCase))
                .SelectMany(p => p.Paths)
                .ToArray();
            return new FlowRuntimeContext { WorkspaceFiles = files };
        }

        return new FlowRuntimeContext
        {
            TestsPassed = string.Equals(node.Stage, "testing", StringComparison.OrdinalIgnoreCase),
            VerifyPassed = string.Equals(node.Stage, "verify", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static HashSet<string> ReachableNodeIds(FlowDefinition def)
    {
        var incoming = def.Edges.GroupBy(e => e.To, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(e => e.From).ToList(), StringComparer.OrdinalIgnoreCase);

        var entry = def.Nodes
            .Where(n => !incoming.ContainsKey(n.Id))
            .Select(n => n.Id)
            .ToList();

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>(entry);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!visited.Add(id))
                continue;

            foreach (var edge in def.Edges.Where(e => e.From.Equals(id, StringComparison.OrdinalIgnoreCase)))
                queue.Enqueue(edge.To);
        }

        return visited;
    }
}
