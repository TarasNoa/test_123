/*
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Libr4.AI.Domain.Entities;

namespace Libr4.AI.Infrastructure.SessionLogging;

/// <summary>
/// Metrics collector interface (TODO: implement)
/// </summary>
public interface IMetricsCollector
{
    void IncrementCounter(string name, double value = 1);
    void RecordTiming(string name, TimeSpan duration);
}

/// <summary>
/// Production-ready session logging service with full audit trail.
/// Stores session data in database for persistence and analysis.
/// </summary>
public sealed class SessionLogger : ISessionLogger
{
    private readonly SessionLogDbContext _dbContext;
    private readonly ILogger<SessionLogger> _logger;
    private readonly IMetricsCollector _metrics;
    private readonly IEncryptionService _encryption;

    public SessionLogger(
        SessionLogDbContext dbContext,
        ILogger<SessionLogger> logger,
        IMetricsCollector metrics,
        IEncryptionService encryption)
    {
        _dbContext = dbContext;
        _logger = logger;
        _metrics = metrics;
        _encryption = encryption;
    }

    /// <summary>
    /// Start a new session with full metadata.
    /// </summary>
    public async Task<SessionLog> StartSessionAsync(
        string userId,
        string? agentId,
        string? projectId,
        SessionOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new SessionOptions();
        var sessionId = Guid.NewGuid().ToString();
        
        var session = new SessionLog
        {
            Id = sessionId,
            UserId = userId,
            AgentId = agentId,
            ProjectId = projectId,
            StartedAt = DateTime.UtcNow,
            Status = SessionStatus.Active,
            Metadata = new Dictionary<string, string>
            {
                ["client_version"] = options.ClientVersion ?? "unknown",
                ["platform"] = options.Platform ?? "unknown",
                ["os"] = options.OperatingSystem ?? "unknown",
                ["model"] = options.Model ?? "default",
                ["session_type"] = options.SessionType ?? "chat"
            },
            TokenCount = 0,
            MessageCount = 0,
            EstimatedCost = 0
        };

        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Session {SessionId} started for user {UserId} with agent {AgentId}",
            sessionId, userId, agentId ?? "none");

        _metrics.IncrementCounter("sessions_started");

        return session;
    }

    /// <summary>
    /// Log a message exchange within a session.
    /// </summary>
    public async Task<SessionMessageLog> LogMessageAsync(
        string sessionId,
        MessageRole role,
        string content,
        string? model = null,
        TokenUsage? tokens = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken ct = default)
    {
        var session = await _dbContext.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        // Encrypt sensitive content if configured
        var processedContent = options?.EncryptContent == true
            ? _encryption.Encrypt(content)
            : content;

        var message = new SessionMessageLog
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = sessionId,
            Role = role,
            Content = processedContent,
            ContentEncrypted = options?.EncryptContent ?? false,
            Model = model ?? session.Metadata.GetValueOrDefault("model", "unknown"),
            Timestamp = DateTime.UtcNow,
            PromptTokens = tokens?.PromptTokens ?? 0,
            CompletionTokens = tokens?.CompletionTokens ?? 0,
            TotalTokens = tokens?.TotalTokens ?? 0,
            Metadata = metadata?.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value?.ToString() ?? "") ?? new Dictionary<string, string>()
        };

        _dbContext.Messages.Add(message);

        // Update session aggregates
        session.MessageCount++;
        session.TokenCount += message.TotalTokens;
        session.LastActivityAt = DateTime.UtcNow;
        session.EstimatedCost += CalculateCost(message.TotalTokens, model);

        await _dbContext.SaveChangesAsync(ct);

        // Record metrics
        _metrics.IncrementCounter("messages_logged", new[] { role.ToString().ToLower() });
        _metrics.RecordHistogram("message_tokens", message.TotalTokens);

        return message;
    }

    /// <summary>
    /// Log a tool execution within a session.
    /// </summary>
    public async Task<SessionToolLog> LogToolExecutionAsync(
        string sessionId,
        string toolName,
        string? toolInput = null,
        string? toolOutput = null,
        bool success = true,
        TimeSpan? duration = null,
        CancellationToken ct = default)
    {
        var toolLog = new SessionToolLog
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = sessionId,
            ToolName = toolName,
            ToolInput = toolInput?.Truncate(10000), // Limit size
            ToolOutput = toolOutput?.Truncate(10000),
            Success = success,
            DurationMs = (long?)duration?.TotalMilliseconds,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.ToolExecutions.Add(toolLog);
        await _dbContext.SaveChangesAsync(ct);

        _metrics.IncrementCounter("tool_executions", new[] { toolName, success ? "success" : "failure" });

        if (!success)
        {
            _logger.LogWarning("Tool {ToolName} failed in session {SessionId}", toolName, sessionId);
        }

        return toolLog;
    }

    /// <summary>
    /// Log an error within a session.
    /// </summary>
    public async Task<SessionErrorLog> LogErrorAsync(
        string sessionId,
        Exception exception,
        string? context = null,
        CancellationToken ct = default)
    {
        var errorLog = new SessionErrorLog
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = sessionId,
            ErrorType = exception.GetType().Name,
            ErrorMessage = exception.Message.Truncate(2000),
            StackTrace = exception.StackTrace?.Truncate(10000),
            Context = context,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.Errors.Add(errorLog);

        // Update session status
        var session = await _dbContext.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        
        if (session != null)
        {
            session.ErrorCount++;
            session.Status = session.ErrorCount > 5 ? SessionStatus.Error : session.Status;
        }

        await _dbContext.SaveChangesAsync(ct);

        _metrics.IncrementCounter("session_errors", new[] { errorLog.ErrorType });

        return errorLog;
    }

    /// <summary>
    /// End a session and record final metrics.
    /// </summary>
    public async Task<SessionLog> EndSessionAsync(
        string sessionId,
        SessionEndReason reason = SessionEndReason.Completed,
        CancellationToken ct = default)
    {
        var session = await _dbContext.Sessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        session.EndedAt = DateTime.UtcNow;
        session.Status = reason switch
        {
            SessionEndReason.Completed => SessionStatus.Completed,
            SessionEndReason.Error => SessionStatus.Error,
            SessionEndReason.Timeout => SessionStatus.Timeout,
            SessionEndReason.Cancelled => SessionStatus.Cancelled,
            _ => SessionStatus.Completed
        };
        session.EndReason = reason.ToString();
        session.DurationSeconds = (long)(session.EndedAt.Value - session.StartedAt).TotalSeconds;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Session {SessionId} ended. Duration: {Duration}s, Messages: {Messages}, Tokens: {Tokens}, Cost: ${Cost:F4}",
            sessionId, session.DurationSeconds, session.MessageCount, session.TokenCount, session.EstimatedCost);

        _metrics.IncrementCounter("sessions_ended", new[] { reason.ToString().ToLower() });
        _metrics.RecordHistogram("session_duration_seconds", session.DurationSeconds);

        return session;
    }

    /// <summary>
    /// Get session history with messages.
    /// </summary>
    public async Task<SessionLog?> GetSessionAsync(
        string sessionId,
        bool includeMessages = true,
        CancellationToken ct = default)
    {
        var query = _dbContext.Sessions.AsQueryable();

        if (includeMessages)
        {
            query = query.Include(s => s.Messages.OrderBy(m => m.Timestamp));
        }

        return await query
            .Include(s => s.ToolExecutions.OrderBy(t => t.Timestamp))
            .Include(s => s.Errors.OrderBy(e => e.Timestamp))
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
    }

    /// <summary>
    /// Get paginated session list for a user.
    /// </summary>
    public async Task<PagedResult<SessionLog>> GetUserSessionsAsync(
        string userId,
        SessionFilter? filter = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        filter ??= new SessionFilter();

        var query = _dbContext.Sessions
            .Where(s => s.UserId == userId);

        // Apply filters
        if (filter.AgentId != null)
            query = query.Where(s => s.AgentId == filter.AgentId);

        if (filter.ProjectId != null)
            query = query.Where(s => s.ProjectId == filter.ProjectId);

        if (filter.Status != null)
            query = query.Where(s => s.Status == filter.Status);

        if (filter.FromDate != null)
            query = query.Where(s => s.StartedAt >= filter.FromDate);

        if (filter.ToDate != null)
            query = query.Where(s => s.StartedAt <= filter.ToDate);

        if (filter.MinTokens != null)
            query = query.Where(s => s.TokenCount >= filter.MinTokens);

        if (filter.HasErrors != null)
            query = filter.HasErrors.Value
                ? query.Where(s => s.ErrorCount > 0)
                : query.Where(s => s.ErrorCount == 0);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<SessionLog>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    /// <summary>
    /// Export session data for analysis or compliance.
    /// </summary>
    public async Task<SessionExport> ExportSessionAsync(
        string sessionId,
        ExportOptions? options = null,
        CancellationToken ct = default)
    {
        var session = await GetSessionAsync(sessionId, true, ct);
        
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        var export = new SessionExport
        {
            SessionId = sessionId,
            ExportedAt = DateTime.UtcNow,
            Format = options?.Format ?? ExportFormat.Json,
            Data = options?.Format switch
            {
                ExportFormat.Markdown => ExportAsMarkdown(session),
                ExportFormat.Html => ExportAsHtml(session),
                _ => ExportAsJson(session)
            }
        };

        return export;
    }

    /// <summary>
    /// Clean up old sessions based on retention policy.
    /// </summary>
    public async Task<CleanupResult> CleanupOldSessionsAsync(
        TimeSpan retentionPeriod,
        bool dryRun = false,
        CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow - retentionPeriod;
        
        var query = _dbContext.Sessions
            .Where(s => s.StartedAt < cutoffDate && s.Status != SessionStatus.Active);

        var count = await query.CountAsync(ct);

        if (!dryRun && count > 0)
        {
            // Cascade delete through related entities
            var sessionsToDelete = await query.ToListAsync(ct);
            _dbContext.Sessions.RemoveRange(sessionsToDelete);
            await _dbContext.SaveChangesAsync(ct);
        }

        return new CleanupResult
        {
            DeletedCount = count,
            DryRun = dryRun,
            CutoffDate = cutoffDate
        };
    }

    private decimal CalculateCost(int tokens, string? model)
    {
        // Simplified pricing per 1K tokens
        var rate = model?.ToLower() switch
        {
            "gpt-4" => 0.03m,
            "gpt-4-turbo" => 0.01m,
            "gpt-3.5-turbo" => 0.0015m,
            "claude-3-opus" => 0.015m,
            "claude-3-sonnet" => 0.003m,
            "claude-3-haiku" => 0.00025m,
            _ => 0.002m
        };

        return tokens * rate / 1000;
    }

    private string ExportAsJson(SessionLog session)
    {
        return JsonSerializer.Serialize(session, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private string ExportAsMarkdown(SessionLog session)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Session: {session.Id}");
        sb.AppendLine($"**User:** {session.UserId}");
        sb.AppendLine($"**Agent:** {session.AgentId ?? "N/A"}");
        sb.AppendLine($"**Started:** {session.StartedAt:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine($"**Duration:** {session.DurationSeconds}s");
        sb.AppendLine($"**Tokens:** {session.TokenCount}");
        sb.AppendLine($"**Estimated Cost:** ${session.EstimatedCost:F4}");
        sb.AppendLine();
        sb.AppendLine("## Messages");
        sb.AppendLine();

        foreach (var msg in session.Messages)
        {
            sb.AppendLine($"### {msg.Role} ({msg.Timestamp:HH:mm:ss})");
            sb.AppendLine($"Model: {msg.Model}");
            sb.AppendLine($"Tokens: {msg.TotalTokens}");
            sb.AppendLine();
            sb.AppendLine(msg.Content);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string ExportAsHtml(SessionLog session)
    {
        // Simplified HTML export
        return $"""
<!DOCTYPE html>
<html>
<head><title>Session {session.Id}</title></head>
<body>
<h1>Session Export</h1>
<p>User: {session.UserId}</p>
<p>Started: {session.StartedAt}</p>
<hr>
{string.Join("", session.Messages.Select(m => $@"<div class='message {m.Role.ToString().ToLower()}'><h3>{m.Role}</h3><pre>{System.Web.HttpUtility.HtmlEncode(m.Content)}</pre></div>"))}
</body>
</html>
""";
    }
}

// Interfaces
public interface ISessionLogger
{
    Task<SessionLog> StartSessionAsync(string userId, string? agentId, string? projectId, SessionOptions? options = null, CancellationToken ct = default);
    Task<SessionMessageLog> LogMessageAsync(string sessionId, MessageRole role, string content, string? model = null, TokenUsage? tokens = null, Dictionary<string, object>? metadata = null, CancellationToken ct = default);
    Task<SessionToolLog> LogToolExecutionAsync(string sessionId, string toolName, string? toolInput = null, string? toolOutput = null, bool success = true, TimeSpan? duration = null, CancellationToken ct = default);
    Task<SessionErrorLog> LogErrorAsync(string sessionId, Exception exception, string? context = null, CancellationToken ct = default);
    Task<SessionLog> EndSessionAsync(string sessionId, SessionEndReason reason = SessionEndReason.Completed, CancellationToken ct = default);
    Task<SessionLog?> GetSessionAsync(string sessionId, bool includeMessages = true, CancellationToken ct = default);
    Task<PagedResult<SessionLog>> GetUserSessionsAsync(string userId, SessionFilter? filter = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<SessionExport> ExportSessionAsync(string sessionId, ExportOptions? options = null, CancellationToken ct = default);
    Task<CleanupResult> CleanupOldSessionsAsync(TimeSpan retentionPeriod, bool dryRun = false, CancellationToken ct = default);
}

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

// Enums and Records
public enum MessageRole { System, User, Assistant, Tool }
public enum SessionStatus { Active, Completed, Error, Timeout, Cancelled }
public enum SessionEndReason { Completed, Error, Timeout, Cancelled, UserInitiated }
public enum ExportFormat { Json, Markdown, Html }

public record TokenUsage(int PromptTokens, int CompletionTokens, int TotalTokens);
public record SessionOptions(
    string? ClientVersion = null,
    string? Platform = null,
    string? OperatingSystem = null,
    string? Model = null,
    string? SessionType = null,
    bool EncryptContent = false);
public record SessionFilter(
    string? AgentId = null,
    string? ProjectId = null,
    SessionStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int? MinTokens = null,
    bool? HasErrors = null);
public record ExportOptions(ExportFormat Format = ExportFormat.Json);
public record SessionExport(string SessionId, DateTime ExportedAt, ExportFormat Format, string Data);
public record CleanupResult(int DeletedCount, bool DryRun, DateTime CutoffDate);
public record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = new List<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
*/

// Extension
public static class StringExtensions
{
    public static string Truncate(this string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
