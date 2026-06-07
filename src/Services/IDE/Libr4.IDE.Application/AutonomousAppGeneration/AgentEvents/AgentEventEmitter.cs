using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;

/// <summary>
/// Event emitter for agent events with persistence
/// TODO: Replace with proper event bus (e.g., MassTransit, RabbitMQ, or SignalR)
/// </summary>
public class AgentEventEmitter : IAgentEventEmitter
{
    private readonly ILogger<AgentEventEmitter> _logger;
    private readonly List<AgentEvent> _events = new();
    private readonly object _lock = new();

    public AgentEventEmitter(ILogger<AgentEventEmitter> logger)
    {
        _logger = logger;
    }

    public Task EmitBuildStartAsync(Guid runId, string command)
    {
        var evt = new AgentEvent(AgentEventType.BuildStart, runId, command: command);
        PersistEvent(evt);
        
        _logger.LogInformation("[Build Start] RunId: {RunId}, Command: {Command}", runId, command);
        return Task.CompletedTask;
    }

    public Task EmitBuildCompleteAsync(Guid runId, string command, string output, int exitCode, long durationMs)
    {
        var evt = new AgentEvent(AgentEventType.BuildComplete, runId, command, output, exitCode, durationMs);
        PersistEvent(evt);
        
        _logger.LogInformation(
            "[Build Complete] RunId: {RunId}, ExitCode: {ExitCode}, Duration: {DurationMs}ms",
            runId,
            exitCode,
            durationMs);
        return Task.CompletedTask;
    }

    public Task EmitTestStartAsync(Guid runId, string command)
    {
        var evt = new AgentEvent(AgentEventType.TestStart, runId, command: command);
        PersistEvent(evt);
        
        _logger.LogInformation("[Test Start] RunId: {RunId}, Command: {Command}", runId, command);
        return Task.CompletedTask;
    }

    public Task EmitTestCompleteAsync(Guid runId, string command, string output, int exitCode, long durationMs)
    {
        var evt = new AgentEvent(AgentEventType.TestComplete, runId, command, output, exitCode, durationMs);
        PersistEvent(evt);
        
        _logger.LogInformation(
            "[Test Complete] RunId: {RunId}, ExitCode: {ExitCode}, Duration: {DurationMs}ms",
            runId,
            exitCode,
            durationMs);
        return Task.CompletedTask;
    }

    public Task EmitSecurityScanStartAsync(Guid runId, string command)
    {
        var evt = new AgentEvent(AgentEventType.SecurityScanStart, runId, command: command);
        PersistEvent(evt);
        
        _logger.LogInformation("[Security Scan Start] RunId: {RunId}, Command: {Command}", runId, command);
        return Task.CompletedTask;
    }

    public Task EmitSecurityScanCompleteAsync(Guid runId, string command, string output, int exitCode, long durationMs)
    {
        var evt = new AgentEvent(AgentEventType.SecurityScanComplete, runId, command, output, exitCode, durationMs);
        PersistEvent(evt);
        
        _logger.LogInformation(
            "[Security Scan Complete] RunId: {RunId}, ExitCode: {ExitCode}, Duration: {DurationMs}ms",
            runId,
            exitCode,
            durationMs);
        return Task.CompletedTask;
    }

    public Task EmitTerminalOutputAsync(Guid runId, string command, string output)
    {
        var evt = new AgentEvent(AgentEventType.TerminalOutput, runId, command, output);
        PersistEvent(evt);
        
        _logger.LogDebug("[Terminal Output] RunId: {RunId}, Command: {Command}", runId, command);
        return Task.CompletedTask;
    }

    public Task EmitBrowserLaunchAsync(Guid runId, string sessionId)
    {
        var evt = new AgentEvent(AgentEventType.BrowserLaunch, runId, command: sessionId);
        PersistEvent(evt);
        
        _logger.LogInformation("[Browser Launch] RunId: {RunId}, SessionId: {SessionId}", runId, sessionId);
        return Task.CompletedTask;
    }

    public Task EmitBrowserNavigateAsync(Guid runId, string sessionId, string url)
    {
        var evt = new AgentEvent(AgentEventType.BrowserNavigate, runId, command: $"{sessionId}:{url}");
        PersistEvent(evt);
        
        _logger.LogInformation("[Browser Navigate] RunId: {RunId}, SessionId: {SessionId}, URL: {Url}", runId, sessionId, url);
        return Task.CompletedTask;
    }

    public Task EmitBrowserScreenshotAsync(Guid runId, string sessionId)
    {
        var evt = new AgentEvent(AgentEventType.BrowserScreenshot, runId, command: sessionId);
        PersistEvent(evt);
        
        _logger.LogInformation("[Browser Screenshot] RunId: {RunId}, SessionId: {SessionId}", runId, sessionId);
        return Task.CompletedTask;
    }

    public Task EmitBrowserExecuteJavaScriptAsync(Guid runId, string sessionId, string script)
    {
        var evt = new AgentEvent(AgentEventType.BrowserExecuteJavaScript, runId, command: $"{sessionId}:{script}");
        PersistEvent(evt);
        
        _logger.LogInformation("[Browser Execute JavaScript] RunId: {RunId}, SessionId: {SessionId}", runId, sessionId);
        return Task.CompletedTask;
    }

    public Task EmitBrowserCloseAsync(Guid runId, string sessionId)
    {
        var evt = new AgentEvent(AgentEventType.BrowserClose, runId, command: sessionId);
        PersistEvent(evt);
        
        _logger.LogInformation("[Browser Close] RunId: {RunId}, SessionId: {SessionId}", runId, sessionId);
        return Task.CompletedTask;
    }

    public Task EmitBrowserToolAsync(Guid runId, string toolName, string sessionId, bool success, string? detail = null)
    {
        var eventType = MapBrowserToolEventType(toolName);
        var command = string.IsNullOrWhiteSpace(detail) ? sessionId : $"{sessionId}:{detail}";
        var evt = new AgentEvent(eventType, runId, command: command, exitCode: success ? 0 : 1);
        PersistEvent(evt);

        _logger.LogInformation(
            "[Browser {Tool}] RunId: {RunId}, SessionId: {SessionId}, Success: {Success}",
            toolName,
            runId,
            sessionId,
            success);
        return Task.CompletedTask;
    }

    public Task EmitRuntimeNdjsonAsync(Guid runId, string eventType, string payloadJson)
    {
        var evt = new AgentEvent(AgentEventType.RuntimeNdjson, runId, command: eventType, output: payloadJson);
        PersistEvent(evt);
        return Task.CompletedTask;
    }

    public event Func<AgentEvent, Task>? EventPublished;

    private static AgentEventType MapBrowserToolEventType(string toolName) => toolName switch
    {
        "browser_launch" => AgentEventType.BrowserLaunch,
        "browser_navigate" => AgentEventType.BrowserNavigate,
        "browser_screenshot" => AgentEventType.BrowserScreenshot,
        "browser_execute_js" => AgentEventType.BrowserExecuteJavaScript,
        "browser_close" => AgentEventType.BrowserClose,
        "browser_snapshot" => AgentEventType.BrowserSnapshot,
        "browser_click" => AgentEventType.BrowserClick,
        "browser_type" => AgentEventType.BrowserType,
        "browser_scroll" => AgentEventType.BrowserScroll,
        "browser_wait" => AgentEventType.BrowserWait,
        "browser_console" => AgentEventType.BrowserConsole,
        "browser_get_content" => AgentEventType.BrowserGetContent,
        "browser_extract" => AgentEventType.BrowserExtract,
        "browser_record_start" => AgentEventType.BrowserRecordStart,
        "browser_record_stop" => AgentEventType.BrowserRecordStop,
        _ => AgentEventType.RuntimeNdjson
    };

    private void PersistEvent(AgentEvent evt)
    {
        lock (_lock)
        {
            _events.Add(evt);
        }

        var handler = EventPublished;
        if (handler is not null)
            _ = Task.Run(() => handler(evt));
    }

    /// <summary>
    /// Gets all events for a specific run
    /// </summary>
    public Task<AgentEvent[]> GetEventsForRun(Guid runId)
    {
        // Try in-memory first
        lock (_lock)
        {
            var events = _events.Where(e => e.RunId == runId).ToArray();
            if (events.Length > 0)
            {
                return Task.FromResult(events);
            }
        }

        // TODO: Try repository when circular dependency is resolved
        // return await _repository.GetEventsForRunAsync(runId);
        return Task.FromResult(Array.Empty<AgentEvent>());
    }

    /// <summary>
    /// Clears all events for a specific run
    /// </summary>
    public Task ClearEventsForRun(Guid runId)
    {
        lock (_lock)
        {
            _events.RemoveAll(e => e.RunId == runId);
        }
        
        // TODO: Clear from repository when circular dependency is resolved
        // await _repository.ClearEventsForRunAsync(runId);
        return Task.CompletedTask;
    }
}
