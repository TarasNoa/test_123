namespace Libr4.IDE.Application.AgentEvents;

public interface IAgentStreamEmitter
{
    Task BroadcastShadowBuildAsync(string workspaceId, string status, IEnumerable<BuildStreamError> errors, TimeSpan? duration, int attempt);
}

public class BuildStreamError
{
    public string File { get; set; } = "";
    public int Line { get; set; }
    public string Message { get; set; } = "";
    public string Code { get; set; } = "";
}
