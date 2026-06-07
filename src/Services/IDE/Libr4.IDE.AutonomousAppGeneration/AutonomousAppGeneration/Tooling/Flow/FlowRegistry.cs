using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;

public interface IFlowRegistry
{
    bool TryGet(string name, out FlowDefinition flow);
    IReadOnlyList<FlowDefinition> All { get; }
}

public sealed class FlowRegistry : IFlowRegistry
{
    private readonly Dictionary<string, FlowDefinition> _flows;

    public FlowRegistry(IOptions<FlowEngineOptions> options, ILogger<FlowRegistry> logger)
    {
        _flows = new Dictionary<string, FlowDefinition>(StringComparer.OrdinalIgnoreCase);
        var dir = ResolveDirectory(options.Value.FlowsDirectory);
        foreach (var doc in FlowYamlLoader.LoadDirectory(dir))
        {
            try
            {
                var def = FlowYamlLoader.ToDefinition(doc);
                _flows[def.Name] = def;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load flow '{Name}'", doc.Name);
            }
        }

        logger.LogInformation("FlowRegistry loaded {Count} flow(s) from {Dir}", _flows.Count, dir);
    }

    public bool TryGet(string name, out FlowDefinition flow) => _flows.TryGetValue(name, out flow!);

    public IReadOnlyList<FlowDefinition> All => _flows.Values.ToList();

    private static string ResolveDirectory(string configured) =>
        Path.IsPathRooted(configured) ? configured : Path.Combine(AppContext.BaseDirectory, configured);
}
