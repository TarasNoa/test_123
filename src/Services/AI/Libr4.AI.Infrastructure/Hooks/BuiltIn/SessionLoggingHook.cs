/*
using Libr4.AI.Infrastructure.SessionLogging;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Hooks.BuiltIn;

/// <summary>
/// Enhanced session logging hook that integrates with SessionLogger.
/// Handles session lifecycle events with full persistence.
/// </summary>
public class SessionLoggingHook : IHook
{
    private readonly ISessionLogger _sessionLogger;
    private readonly ILogger<SessionLoggingHook> _logger;

    public HookType Type => HookType.SessionStart;
    public string Name => "SessionLogging";

    public SessionLoggingHook(
        ISessionLogger sessionLogger,
        ILogger<SessionLoggingHook> logger)
    {
        _sessionLogger = sessionLogger;
        _logger = logger;
    }

    public async Task<HookResult> ExecuteAsync(HookContext context)
    {
        try
        {
            // Start or update session in database
            var session = await _sessionLogger.StartSessionAsync(
                userId: context.UserId ?? "anonymous",
                agentId: context.AgentId,
                projectId: context.ProjectId,
                options: new SessionOptions(
                    ClientVersion: context.Metadata.GetValueOrDefault("client_version"),
                    Platform: context.Metadata.GetValueOrDefault("platform"),
                    OperatingSystem: context.Metadata.GetValueOrDefault("os"),
                    Model: context.Metadata.GetValueOrDefault("model"),
                    SessionType: context.Metadata.GetValueOrDefault("session_type") ?? "interactive"
                ),
                ct: CancellationToken.None);

            // Store session ID in context for other hooks
            context.Result = new Dictionary<string, object>
            {
                ["session_log_id"] = session.Id,
                ["started_at"] = session.StartedAt
            };

            _logger.LogInformation(
                "Session {SessionId} logged to database. Log ID: {LogId}, User: {UserId}, Agent: {AgentId}",
                context.SessionId,
                session.Id,
                context.UserId ?? "anonymous",
                context.AgentId ?? "N/A"
            );

            return new HookResult
            {
                ShouldContinue = true,
                ModifiedResult = context.Result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log session start for {SessionId}", context.SessionId);
            // Continue even if logging fails - don't block the session
            return new HookResult { ShouldContinue = true };
        }
    }
}

/// <summary>
/// Hook for logging session end events.
/// </summary>
public class SessionEndLoggingHook : IHook
{
    private readonly ISessionLogger _sessionLogger;
    private readonly ILogger<SessionEndLoggingHook> _logger;

    public HookType Type => HookType.SessionEnd;
    public string Name => "SessionEndLogging";

    public SessionEndLoggingHook(
        ISessionLogger sessionLogger,
        ILogger<SessionEndLoggingHook> logger)
    {
        _sessionLogger = sessionLogger;
        _logger = logger;
    }

    public async Task<HookResult> ExecuteAsync(HookContext context)
    {
        try
        {
            // Try to get session log ID from context
            var sessionLogId = context.Metadata.GetValueOrDefault("session_log_id");
            
            if (!string.IsNullOrEmpty(sessionLogId))
            {
                var endReason = context.Metadata.GetValueOrDefault("end_reason") switch
                {
                    "error" => SessionEndReason.Error,
                    "timeout" => SessionEndReason.Timeout,
                    "cancelled" => SessionEndReason.Cancelled,
                    _ => SessionEndReason.Completed
                };

                await _sessionLogger.EndSessionAsync(sessionLogId, endReason, CancellationToken.None);

                _logger.LogInformation(
                    "Session {SessionId} ended and logged. Reason: {Reason}",
                    context.SessionId,
                    endReason);
            }
            else
            {
                _logger.LogWarning(
                    "Session {SessionId} ended but no log ID found in context",
                    context.SessionId);
            }

            return new HookResult { ShouldContinue = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log session end for {SessionId}", context.SessionId);
            return new HookResult { ShouldContinue = true };
        }
    }
}

/// <summary>
/// Hook for logging message exchanges.
/// </summary>
public class MessageLoggingHook : IHook
{
    private readonly ISessionLogger _sessionLogger;
    private readonly ILogger<MessageLoggingHook> _logger;

    public HookType Type => HookType.MessageReceived;
    public string Name => "MessageLogging";

    public MessageLoggingHook(
        ISessionLogger sessionLogger,
        ILogger<MessageLoggingHook> logger)
    {
        _sessionLogger = sessionLogger;
        _logger = logger;
    }

    public async Task<HookResult> ExecuteAsync(HookContext context)
    {
        try
        {
            var sessionLogId = context.Metadata.GetValueOrDefault("session_log_id");
            if (string.IsNullOrEmpty(sessionLogId))
            {
                return new HookResult { ShouldContinue = true };
            }

            var role = context.Metadata.GetValueOrDefault("message_role") switch
            {
                "user" => MessageRole.User,
                "assistant" => MessageRole.Assistant,
                "system" => MessageRole.System,
                "tool" => MessageRole.Tool,
                _ => MessageRole.User
            };

            var content = context.Metadata.GetValueOrDefault("message_content") ?? "";
            var model = context.Metadata.GetValueOrDefault("model");
            
            // Parse token usage if available
            TokenUsage? tokens = null;
            if (int.TryParse(context.Metadata.GetValueOrDefault("prompt_tokens"), out var promptTokens) &&
                int.TryParse(context.Metadata.GetValueOrDefault("completion_tokens"), out var completionTokens))
            {
                tokens = new TokenUsage(promptTokens, completionTokens, promptTokens + completionTokens);
            }

            await _sessionLogger.LogMessageAsync(
                sessionLogId,
                role,
                content,
                model,
                tokens,
                metadata: context.Metadata.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value),
                ct: CancellationToken.None);

            return new HookResult { ShouldContinue = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log message for session {SessionId}", context.SessionId);
            return new HookResult { ShouldContinue = true };
        }
    }
}
*/
