using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;

public static class AgentSpecLoader
{
    public static AgentSpecDocument LoadFromFile(string path)
    {
        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        var doc = deserializer.Deserialize<AgentSpecDocument>(yaml)
                  ?? throw new InvalidOperationException($"empty agent spec: {path}");
        if (string.IsNullOrWhiteSpace(doc.Name))
            doc.Name = Path.GetFileNameWithoutExtension(path).Replace(".agent", string.Empty);
        return doc;
    }

    public static IReadOnlyList<AgentSpecDocument> LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return Array.Empty<AgentSpecDocument>();

        return Directory.EnumerateFiles(directory, "*.agent.yaml", SearchOption.TopDirectoryOnly)
            .Select(LoadFromFile)
            .ToList();
    }

    public static AgentSpecDocument GetMergedDocument(
        AgentSpecDocument doc,
        IReadOnlyDictionary<string, AgentSpecDocument> parents)
    {
        if (string.IsNullOrWhiteSpace(doc.Extend)
            || !parents.TryGetValue(doc.Extend, out var parent))
        {
            return doc;
        }

        var parentMerged = GetMergedDocument(parent, parents);
        return Merge(parentMerged, doc);
    }

    public static AgentSpec Resolve(AgentSpecDocument doc, IReadOnlyDictionary<string, AgentSpecDocument> parents)
    {
        var merged = GetMergedDocument(doc, parents);
        if (!string.IsNullOrWhiteSpace(doc.Extend)
            && parents.TryGetValue(doc.Extend, out var parent))
        {
            var parentSpec = Resolve(parent, parents);
            return ToSpec(merged, parentSpec.MaxTurns);
        }

        return ToSpec(merged, 12);
    }

    private static AgentSpecDocument Merge(AgentSpecDocument parent, AgentSpecDocument child)
    {
        return new AgentSpecDocument
        {
            Name = string.IsNullOrWhiteSpace(child.Name) ? parent.Name : child.Name,
            Extend = child.Extend ?? parent.Extend,
            Model = child.Model ?? parent.Model,
            MaxTurns = child.MaxTurns ?? parent.MaxTurns,
            MaxTokens = child.MaxTokens ?? parent.MaxTokens,
            Toolset = child.Toolset.Count > 0 ? child.Toolset : parent.Toolset,
            Instruction = string.IsNullOrWhiteSpace(child.Instruction) ? parent.Instruction : child.Instruction,
            Permissions = child.Permissions ?? parent.Permissions,
            Backend = child.Backend ?? parent.Backend,
            BackendConfig = child.BackendConfig ?? parent.BackendConfig,
            Browser = child.Browser ?? parent.Browser
        };
    }

    private static AgentSpec ToSpec(AgentSpecDocument doc, int defaultTurns)
    {
        var backend = string.IsNullOrWhiteSpace(doc.Backend)
            ? AgentBackendDescriptor.Native
            : AgentBackendDescriptor.Parse(doc.Backend, doc.BackendConfig);

        return new AgentSpec
        {
            Name = doc.Name,
            Model = doc.Model,
            MaxTurns = doc.MaxTurns ?? defaultTurns,
            MaxTokens = doc.MaxTokens,
            Toolset = doc.Toolset.Count == 0 ? Array.Empty<string>() : doc.Toolset.ToArray(),
            Instruction = doc.Instruction?.Trim() ?? string.Empty,
            Permissions = doc.Permissions,
            Backend = backend
        };
    }
}
