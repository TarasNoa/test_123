namespace Libr4.IDE.Application.AgentEvents;

public interface IAgentSpawnerService
{
    Task<string> SpawnAgentAsync(string agentType, string task, string[] targetFiles, string? parentAgentId = null);
    Task<bool> KillAgentAsync(string agentId);
    Task<AgentInfo?> GetAgentAsync(string agentId);
}

public class AgentInfo
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string Task { get; set; } = "";
    public string[] TargetFiles { get; set; } = Array.Empty<string>();
    public string? ParentAgentId { get; set; }
}
