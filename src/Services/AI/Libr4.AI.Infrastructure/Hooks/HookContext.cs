namespace Libr4.AI.Infrastructure.Hooks;

public class HookContext
{
    public string SessionId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public object? Result { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? UserId { get; set; }
    public string? AgentId { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
