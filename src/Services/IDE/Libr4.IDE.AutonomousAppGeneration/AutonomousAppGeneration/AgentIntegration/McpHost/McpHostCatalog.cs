using System.Collections.Concurrent;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;

public sealed class McpHostCatalog : IMcpHostCatalog
{
    private readonly IMcpToolRegistry _registry;
    private readonly ConcurrentDictionary<string, List<McpCatalogTool>> _discovered = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<McpCatalogResource> _resources =
    [
        new("memory://libr4/stats", "libr4-internal", McpHostTransportKind.Internal, "Memory Statistics", "application/json"),
        new("runs://active", "libr4-internal", McpHostTransportKind.Internal, "Active Generation Runs", "application/json"),
    ];
    private readonly List<McpCatalogPrompt> _prompts =
    [
        new("code_review", "libr4-internal", McpHostTransportKind.Internal, "Review generated code with Libr4 context"),
        new("verify_repair", "libr4-internal", McpHostTransportKind.Internal, "Repair hint prompt after verify failure"),
    ];

    public McpHostCatalog(IMcpToolRegistry registry) => _registry = registry;

    public IReadOnlyList<McpCatalogTool> ListTools()
    {
        var tools = _registry.ListTools()
            .Select(t => new McpCatalogTool(
                t.ToolName,
                t.ServerProfileKey,
                ResolveTransport(t.ServerProfileKey),
                t.Description,
                t.Scopes.ToList()))
            .ToList();

        foreach (var extra in _discovered.Values)
            tools.AddRange(extra);

        return tools
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<McpCatalogResource> ListResources() => _resources;

    public IReadOnlyList<McpCatalogPrompt> ListPrompts() => _prompts;

    public void RegisterDiscoveredTools(
        string profileKey,
        McpHostTransportKind transport,
        IReadOnlyList<McpCatalogTool> tools)
    {
        var mapped = tools
            .Select(t => t with { ServerProfileKey = profileKey, Transport = transport })
            .ToList();
        _discovered[profileKey] = mapped;
    }

    private static McpHostTransportKind ResolveTransport(string profileKey) =>
        profileKey.Equals("libr4-internal", StringComparison.OrdinalIgnoreCase)
        || profileKey.Equals("mcp-meta", StringComparison.OrdinalIgnoreCase)
            ? McpHostTransportKind.Internal
            : McpHostTransportKind.Stdio;
}
