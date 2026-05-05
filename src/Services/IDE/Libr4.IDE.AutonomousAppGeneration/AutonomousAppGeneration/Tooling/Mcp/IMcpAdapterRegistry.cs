namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Mcp;

public sealed record McpDatasourceAdapter(string Id, string DisplayName, string Category, bool EnabledByDefault);

public interface IMcpAdapterRegistry
{
    void Register(McpDatasourceAdapter adapter);
    IReadOnlyList<McpDatasourceAdapter> GetAll();
    bool IsRegistered(string id);
}
