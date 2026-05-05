namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Mcp;

public sealed class McpAdapterRegistry : IMcpAdapterRegistry
{
    private readonly Dictionary<string, McpDatasourceAdapter> _items = new(StringComparer.OrdinalIgnoreCase);

    public void Register(McpDatasourceAdapter adapter)
    {
        if (string.IsNullOrWhiteSpace(adapter.Id))
            throw new ArgumentException("Adapter id is required.", nameof(adapter));
        _items[adapter.Id] = adapter;
    }

    public IReadOnlyList<McpDatasourceAdapter> GetAll() => _items.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToArray();

    public bool IsRegistered(string id) => !string.IsNullOrWhiteSpace(id) && _items.ContainsKey(id);
}
