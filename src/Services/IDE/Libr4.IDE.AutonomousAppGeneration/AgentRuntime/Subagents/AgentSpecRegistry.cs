using Libr4.IDE.Application.Obscura;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;

public interface IAgentSpecRegistry
{
    bool TryGet(string name, out AgentSpec spec);
    IReadOnlyList<AgentSpec> All { get; }
}

public sealed class AgentSpecRegistry : IAgentSpecRegistry
{
    private readonly Dictionary<string, AgentSpec> _specs;

    public AgentSpecRegistry(
        IOptions<AgentSpecOptions> options,
        ILogger<AgentSpecRegistry> logger,
        ISubagentObscuraIntegration? obscuraIntegration = null)
    {
        _specs = new Dictionary<string, AgentSpec>(StringComparer.OrdinalIgnoreCase);
        var opts = options.Value;
        var byName = new Dictionary<string, AgentSpecDocument>(StringComparer.OrdinalIgnoreCase);

        var dir = ResolveSpecsDirectory(opts.SpecsDirectory);
        if (Directory.Exists(dir))
        {
            foreach (var doc in AgentSpecLoader.LoadDirectory(dir))
                byName[doc.Name] = doc;
        }

        if (!string.IsNullOrWhiteSpace(opts.EvolvedSpecsDirectory))
        {
            var evolvedDir = ResolveSpecsDirectory(opts.EvolvedSpecsDirectory);
            if (Directory.Exists(evolvedDir))
            {
                foreach (var doc in AgentSpecLoader.LoadDirectory(evolvedDir))
                    byName[doc.Name] = doc;
            }
        }

        if (opts.SubAgents.Count > 0)
        {
            foreach (var doc in opts.SubAgents)
                byName[doc.Name] = doc;
        }

        foreach (var doc in byName.Values)
        {
            try
            {
                var mergedDoc = AgentSpecLoader.GetMergedDocument(doc, byName);
                var spec = AgentSpecLoader.Resolve(doc, byName);
                _specs[spec.Name] = spec;

                if (obscuraIntegration is not null && mergedDoc.Browser is not null)
                {
                    var browserConfig = SubagentBrowserConfigMapper.Map(spec.Name, mergedDoc.Browser);
                    obscuraIntegration.RegisterSubagentBrowserConfig(spec.Name, browserConfig);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load agent spec '{Name}'", doc.Name);
            }
        }

        logger.LogInformation("AgentSpecRegistry loaded {Count} spec(s) from {Dir}", _specs.Count, dir);
    }

    public bool TryGet(string name, out AgentSpec spec) => _specs.TryGetValue(name, out spec!);

    public IReadOnlyList<AgentSpec> All => _specs.Values.ToList();

    private static string ResolveSpecsDirectory(string configured)
    {
        if (Path.IsPathRooted(configured))
            return configured;

        return Path.Combine(AppContext.BaseDirectory, configured);
    }
}
