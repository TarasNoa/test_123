namespace Libr4.Shared.Contracts.Mcp;

/// <summary>
/// Interface for MCP (Model Context Protocol) datasource adapters.
/// Adapters provide access to external datasources like Google Drive, Figma, Slack, Jira.
/// </summary>
public interface IMcpAdapter
{
    /// <summary>
    /// Unique identifier for the adapter.
    /// </summary>
    string AdapterId { get; }

    /// <summary>
    /// Human-readable name of the datasource.
    /// </summary>
    string DatasourceName { get; }

    /// <summary>
    /// Type of datasource (e.g., "google_drive", "figma", "slack", "jira").
    /// </summary>
    string DatasourceType { get; }

    /// <summary>
    /// Checks if the adapter is connected and authenticated.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connected, false otherwise.</returns>
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to the datasource with the provided credentials.
    /// </summary>
    /// <param name="credentials">Connection credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection succeeded.</returns>
    Task<bool> ConnectAsync(McpCredentials credentials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the datasource.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the datasource for data matching the query.
    /// </summary>
    /// <param name="query">Query string or structured query object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query results.</returns>
    Task<McpQueryResult> QueryAsync(McpQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists available resources in the datasource.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available resources.</returns>
    Task<IReadOnlyList<McpResource>> ListResourcesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Credentials for connecting to an MCP datasource.
/// </summary>
public record McpCredentials
{
    /// <summary>
    /// API token or access token.
    /// </summary>
    public string? ApiToken { get; init; }

    /// <summary>
    /// OAuth token.
    /// </summary>
    public string? OAuthToken { get; init; }

    /// <summary>
    /// Username for basic auth.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Password for basic auth.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Additional custom credentials.
    /// </summary>
    public Dictionary<string, string> CustomCredentials { get; init; } = new();
}

/// <summary>
/// Query for an MCP datasource.
/// </summary>
public record McpQuery
{
    /// <summary>
    /// Query string (e.g., search query, file path, etc.).
    /// </summary>
    public string QueryString { get; init; } = string.Empty;

    /// <summary>
    /// Query type (e.g., "search", "get", "list").
    /// </summary>
    public string QueryType { get; init; } = "search";

    /// <summary>
    /// Filters to apply to the query.
    /// </summary>
    public Dictionary<string, string> Filters { get; init; } = new();

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    public int MaxResults { get; init; } = 50;
}

/// <summary>
/// Result of an MCP query.
/// </summary>
public record McpQueryResult
{
    /// <summary>
    /// Query results as structured data.
    /// </summary>
    public List<McpResource> Resources { get; init; } = new();

    /// <summary>
    /// Raw response data.
    /// </summary>
    public string? RawResponse { get; init; }

    /// <summary>
    /// Whether the query was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the query failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Total number of results available (may be more than returned).
    /// </summary>
    public int TotalCount { get; init; }
}

/// <summary>
/// Resource from an MCP datasource.
/// </summary>
public record McpResource
{
    /// <summary>
    /// Unique identifier for the resource.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Resource name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Resource type (e.g., "file", "folder", "document", "channel").
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Resource content or URL.
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// URL to the resource.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Metadata about the resource.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// When the resource was last modified.
    /// </summary>
    public DateTime? LastModified { get; init; }
}

/// <summary>
/// Registry for managing MCP adapters.
/// </summary>
public interface IMcpAdapterRegistry
{
    /// <summary>
    /// Registers an MCP adapter.
    /// </summary>
    /// <param name="adapter">Adapter to register.</param>
    void RegisterAdapter(IMcpAdapter adapter);

    /// <summary>
    /// Unregisters an MCP adapter.
    /// </summary>
    /// <param name="adapterId">ID of the adapter to unregister.</param>
    void UnregisterAdapter(string adapterId);

    /// <summary>
    /// Gets an adapter by ID.
    /// </summary>
    /// <param name="adapterId">ID of the adapter.</param>
    /// <returns>The adapter, or null if not found.</returns>
    IMcpAdapter? GetAdapter(string adapterId);

    /// <summary>
    /// Gets all registered adapters.
    /// </summary>
    /// <returns>List of all registered adapters.</returns>
    IReadOnlyList<IMcpAdapter> GetAllAdapters();

    /// <summary>
    /// Gets adapters by datasource type.
    /// </summary>
    /// <param name="datasourceType">Type of datasource.</param>
    /// <returns>List of adapters of the specified type.</returns>
    IReadOnlyList<IMcpAdapter> GetAdaptersByType(string datasourceType);
}
