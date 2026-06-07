using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;

public static class FlowYamlLoader
{
    public static FlowDefinitionDocument LoadFromFile(string path)
    {
        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        var doc = deserializer.Deserialize<FlowDefinitionDocument>(yaml)
                  ?? throw new InvalidOperationException($"empty flow: {path}");
        if (string.IsNullOrWhiteSpace(doc.Name))
            doc.Name = Path.GetFileNameWithoutExtension(path).Replace(".flow", string.Empty);
        return doc;
    }

    public static IReadOnlyList<FlowDefinitionDocument> LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return Array.Empty<FlowDefinitionDocument>();

        return Directory.EnumerateFiles(directory, "*.flow.yaml", SearchOption.TopDirectoryOnly)
            .Select(LoadFromFile)
            .ToList();
    }

    public static FlowDefinition ToDefinition(FlowDefinitionDocument doc) =>
        new()
        {
            Name = doc.Name,
            Description = doc.Description,
            Nodes = doc.Nodes.Select(ToNode).ToArray(),
            Edges = doc.Edges.Select(ToEdge).ToArray()
        };

    private static FlowNode ToNode(FlowNodeDocument node) =>
        new()
        {
            Id = node.Id,
            Type = Enum.TryParse<FlowNodeType>(node.Type, ignoreCase: true, out var type) ? type : FlowNodeType.Stage,
            Stage = node.Stage,
            Phase = node.Phase,
            Preconditions = node.Preconditions.Select(p => new FlowPrecondition
            {
                Kind = p.Kind,
                Paths = p.Paths
            }).ToArray(),
            MaxRetries = node.MaxRetries ?? 1
        };

    private static FlowEdge ToEdge(FlowEdgeDocument edge) =>
        new()
        {
            From = edge.From,
            To = edge.To,
            On = edge.On.Equals("failure", StringComparison.OrdinalIgnoreCase)
                ? FlowEdgeOutcome.Failure
                : FlowEdgeOutcome.Success,
            Action = TryParseAction(edge.Action),
            MaxRetries = edge.MaxRetries ?? 1
        };

    private static FlowFailureAction? TryParseAction(string? action) =>
        action is null ? null
            : Enum.TryParse<FlowFailureAction>(action, ignoreCase: true, out var parsed) ? parsed : null;
}
