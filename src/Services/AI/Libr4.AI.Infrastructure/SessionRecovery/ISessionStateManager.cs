namespace Libr4.AI.Infrastructure.SessionRecovery;

/// <summary>
/// Manages external state for LLM sessions - file-based persistence
/// </summary>
public interface ISessionStateManager
{
    /// <summary>
    /// Get or create session state
    /// </summary>
    Task<SessionState> GetSessionAsync(string userId, Guid? sessionId = null);
    
    /// <summary>
    /// Update session state
    /// </summary>
    Task UpdateSessionAsync(SessionState state);
    
    /// <summary>
    /// Add task to session
    /// </summary>
    Task AddTaskAsync(Guid sessionId, SessionTask task);
    
    /// <summary>
    /// Update task status
    /// </summary>
    Task UpdateTaskStatusAsync(Guid sessionId, Guid taskId, TaskStatus status);
    
    /// <summary>
    /// Log prompt event
    /// </summary>
    Task LogPromptAsync(Guid sessionId, string prompt, string response, Dictionary<string, object>? context = null);
    
    /// <summary>
    /// Archive completed tasks
    /// </summary>
    Task ArchiveCompletedTasksAsync(Guid sessionId);
    
    /// <summary>
    /// Get current context for LLM (formatted as markdown)
    /// </summary>
    Task<string> GetContextAsync(Guid sessionId);
}
