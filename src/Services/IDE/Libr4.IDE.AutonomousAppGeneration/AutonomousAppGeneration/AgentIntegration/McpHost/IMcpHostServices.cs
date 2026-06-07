using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;

public interface IMcpHostCatalog
{
    IReadOnlyList<McpCatalogTool> ListTools();

    IReadOnlyList<McpCatalogResource> ListResources();

    IReadOnlyList<McpCatalogPrompt> ListPrompts();

    void RegisterDiscoveredTools(string profileKey, McpHostTransportKind transport, IReadOnlyList<McpCatalogTool> tools);
}

public interface IMcpRunHostManager
{
    bool IsUnifiedHostEnabled { get; }

    Task<JsonElement> CallToolAsync(
        Guid? runId,
        string profileKey,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        TimeSpan timeout,
        CancellationToken ct);

    void ReleaseRun(Guid runId);

    IReadOnlyList<McpRunHostSessionInfo> ListActiveSessions();

    IReadOnlyList<McpServerDiscoveryResult> DiscoverServers();
}

public interface IMcpExternalServerDiscovery
{
    Task<IReadOnlyList<McpServerDiscoveryResult>> DiscoverAsync(CancellationToken ct = default);
}
