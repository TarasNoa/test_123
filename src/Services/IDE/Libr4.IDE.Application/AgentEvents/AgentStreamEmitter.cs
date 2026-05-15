using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AgentEvents;

public class AgentStreamEmitter : IAgentStreamEmitter
{
    private readonly ILogger<AgentStreamEmitter> _logger;

    public AgentStreamEmitter(ILogger<AgentStreamEmitter> logger)
    {
        _logger = logger;
    }

    public Task BroadcastShadowBuildAsync(string workspaceId, string status, IEnumerable<BuildStreamError> errors, TimeSpan? duration, int attempt)
    {
        _logger.LogDebug("[ShadowBuild] Workspace: {WorkspaceId}, Status: {Status}, Attempt: {Attempt}", workspaceId, status, attempt);
        return Task.CompletedTask;
    }
}
