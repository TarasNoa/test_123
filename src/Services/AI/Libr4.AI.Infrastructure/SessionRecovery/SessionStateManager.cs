using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.SessionRecovery;

/// <summary>
/// File-based session state manager - persistent external state for LLM
/// </summary>
public class SessionStateManager : ISessionStateManager
{
    private readonly string _basePath;
    private readonly ILogger<SessionStateManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SessionStateManager(ILogger<SessionStateManager> logger)
    {
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), ".session-recovery");
        Directory.CreateDirectory(_basePath);
        _logger = logger;
    }

    public async Task<SessionState> GetSessionAsync(string userId, Guid? sessionId = null)
    {
        var userPath = Path.Combine(_basePath, userId);
        Directory.CreateDirectory(userPath);
        
        if (sessionId.HasValue)
        {
            var sessionFile = Path.Combine(userPath, $"{sessionId.Value}.json");
            if (File.Exists(sessionFile))
            {
                var json = await File.ReadAllTextAsync(sessionFile);
                return JsonSerializer.Deserialize<SessionState>(json, _jsonOptions) 
                    ?? throw new InvalidOperationException("Failed to deserialize session state");
            }
        }

        // Create new session
        var newSession = new SessionState
        {
            SessionId = sessionId ?? Guid.NewGuid(),
            UserId = userId,
            StartedAt = DateTimeOffset.UtcNow,
            LastUpdated = DateTimeOffset.UtcNow
        };
        
        await SaveSessionAsync(newSession);
        return newSession;
    }

    public async Task UpdateSessionAsync(SessionState state)
    {
        state.LastUpdated = DateTimeOffset.UtcNow;
        await SaveSessionAsync(state);
    }

    public async Task AddTaskAsync(Guid sessionId, SessionTask task)
    {
        var state = await LoadSessionByIdAsync(sessionId);
        state.Tasks.Add(task);
        await UpdateSessionAsync(state);
    }

    public async Task UpdateTaskStatusAsync(Guid sessionId, Guid taskId, TaskStatus status)
    {
        var state = await LoadSessionByIdAsync(sessionId);
        var task = state.Tasks.FirstOrDefault(t => t.Id == taskId)
            ?? throw new InvalidOperationException($"Task {taskId} not found");
        
        task.Status = status;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        
        if (status == TaskStatus.Completed)
        {
            state.Tasks.Remove(task);
            state.CompletedTasks.Add(task);
        }
        
        await UpdateSessionAsync(state);
    }

    public async Task LogPromptAsync(Guid sessionId, string prompt, string response, Dictionary<string, object>? context = null)
    {
        var state = await LoadSessionByIdAsync(sessionId);
        state.PromptHistory.Add(new PromptEvent
        {
            Id = Guid.NewGuid(),
            Prompt = prompt,
            Response = response,
            Timestamp = DateTimeOffset.UtcNow,
            Context = context ?? new Dictionary<string, object>()
        });
        await UpdateSessionAsync(state);
    }

    public async Task ArchiveCompletedTasksAsync(Guid sessionId)
    {
        var state = await LoadSessionByIdAsync(sessionId);
        // Tasks are already moved to CompletedTasks when status is set to Completed
        // This is a no-op but kept for API completeness
        await UpdateSessionAsync(state);
    }

    public async Task<string> GetContextAsync(Guid sessionId)
    {
        var state = await LoadSessionByIdAsync(sessionId);
        
        var context = new System.Text.StringBuilder();
        
        // Current tasks (max 150 lines)
        context.AppendLine("# CURRENT_SESSION_TASKS");
        context.AppendLine($"# Session: {state.SessionId}");
        context.AppendLine($"# Last Updated: {state.LastUpdated:yyyy-MM-dd HH:mm:ss}");
        context.AppendLine();
        
        foreach (var task in state.Tasks.Where(t => t.Status != TaskStatus.Completed))
        {
            context.AppendLine($"- [{task.Status}] {task.Title}");
            if (!string.IsNullOrEmpty(task.Description))
            {
                context.AppendLine($"  {task.Description}");
            }
            if (task.Dependencies.Any())
            {
                context.AppendLine($"  Dependencies: {string.Join(", ", task.Dependencies)}");
            }
        }
        
        context.AppendLine();
        context.AppendLine("# PROMPT_HISTORY (Last 10)");
        context.AppendLine();
        
        foreach (var evt in state.PromptHistory.TakeLast(10))
        {
            context.AppendLine($"## {evt.Timestamp:yyyy-MM-dd HH:mm:ss}");
            context.AppendLine(evt.Prompt);
            context.AppendLine();
            context.AppendLine(evt.Response);
            context.AppendLine();
        }
        
        return context.ToString();
    }

    private async Task<SessionState> LoadSessionByIdAsync(Guid sessionId)
    {
        // Find session file by scanning all user directories
        var sessionFiles = Directory.GetFiles(_basePath, "*.json", SearchOption.AllDirectories);
        var sessionFile = sessionFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == sessionId.ToString());
        
        if (sessionFile == null)
            throw new InvalidOperationException($"Session {sessionId} not found");
        
        var json = await File.ReadAllTextAsync(sessionFile);
        return JsonSerializer.Deserialize<SessionState>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize session state");
    }

    private async Task SaveSessionAsync(SessionState state)
    {
        var userPath = Path.Combine(_basePath, state.UserId);
        Directory.CreateDirectory(userPath);
        
        var sessionFile = Path.Combine(userPath, $"{state.SessionId}.json");
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        await File.WriteAllTextAsync(sessionFile, json);
        
        _logger.LogDebug("Saved session state: {SessionId}", state.SessionId);
    }
}
