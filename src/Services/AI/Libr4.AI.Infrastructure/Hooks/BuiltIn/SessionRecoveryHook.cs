using Libr4.AI.Infrastructure.SessionRecovery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Hooks.BuiltIn;

/// <summary>
/// SESSION_RECOVERY Hook - ensures external state synchronization before/after LLM operations
/// Based on the pattern: "External State vs Embedded Memory"
/// </summary>
public class SessionRecoveryHook : IHook
{
    private readonly ISessionStateManager _sessionManager;
    private readonly ILogger<SessionRecoveryHook> _logger;
    private readonly string? _currentUserId;
    private Guid? _currentSessionId;

    public SessionRecoveryHook(
        ISessionStateManager sessionManager,
        ILogger<SessionRecoveryHook> logger,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _sessionManager = sessionManager;
        _logger = logger;
        
        // Extract user ID from HTTP context if available
        _currentUserId = httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value
                      ?? httpContextAccessor?.HttpContext?.User?.FindFirst("user_id")?.Value
                      ?? "default_user";
    }

    public HookType Type => HookType.PreToolUse;
    public string Name => "SessionRecovery";
    public int Priority => 100; // High priority - runs first

    public async Task<HookResult> ExecuteAsync(HookContext context)
    {
        try
        {
            // Get or create session
            var session = await _sessionManager.GetSessionAsync(_currentUserId, _currentSessionId);
            _currentSessionId = session.SessionId;

            // Inject current context into the prompt
            var sessionContext = await _sessionManager.GetContextAsync(session.SessionId);
            
            context.Metadata["session_id"] = session.SessionId.ToString();
            context.Metadata["session_context"] = sessionContext;

            _logger.LogDebug("SessionRecovery: Loaded session {SessionId} with {TaskCount} tasks", 
                session.SessionId, session.Tasks.Count);

            return new HookResult { ShouldContinue = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SessionRecovery: Failed to load session state");
            // Don't block execution if session recovery fails
            return new HookResult { ShouldContinue = true };
        }
    }
}
