namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;

public sealed record McpCatalogTool(
    string Name,
    string ServerProfileKey,
    McpHostTransportKind Transport,
    string? Description,
    IReadOnlyList<string> Scopes);

public sealed record McpCatalogResource(
    string Uri,
    string ServerProfileKey,
    McpHostTransportKind Transport,
    string? Name,
    string? MimeType);

public sealed record McpCatalogPrompt(
    string Name,
    string ServerProfileKey,
    McpHostTransportKind Transport,
    string? Description);

public sealed record McpServerDiscoveryResult(
    string ProfileKey,
    McpHostTransportKind Transport,
    bool Available,
    string? BlockerCode,
    string? Detail,
    int ToolCount,
    int ResourceCount,
    int PromptCount);

public sealed record McpRunHostSessionInfo(
    Guid RunId,
    string ProfileKey,
    McpHostTransportKind Transport,
    DateTime StartedAtUtc,
    DateTime LastUsedAtUtc,
    int CallCount);
