namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;

public interface IAgentEventEmitter
{
    Task EmitBuildStartAsync(Guid runId, string command);
    Task EmitBuildCompleteAsync(Guid runId, string command, string output, int exitCode, long durationMs);
    Task EmitTestStartAsync(Guid runId, string command);
    Task EmitTestCompleteAsync(Guid runId, string command, string output, int exitCode, long durationMs);
    Task EmitSecurityScanStartAsync(Guid runId, string command);
    Task EmitSecurityScanCompleteAsync(Guid runId, string command, string output, int exitCode, long durationMs);
    Task EmitTerminalOutputAsync(Guid runId, string command, string output);
    
    // Obscura browser events
    Task EmitBrowserLaunchAsync(Guid runId, string sessionId);
    Task EmitBrowserNavigateAsync(Guid runId, string sessionId, string url);
    Task EmitBrowserScreenshotAsync(Guid runId, string sessionId);
    Task EmitBrowserExecuteJavaScriptAsync(Guid runId, string sessionId, string script);
    Task EmitBrowserCloseAsync(Guid runId, string sessionId);
}
