using Libr4.AI.Domain.Terminal;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.Terminal;

/// <summary>
/// Terminal service using Docker for command execution in shadow workspaces
/// </summary>
public sealed class DockerTerminalService : ITerminalService
{
    private readonly ILogger<DockerTerminalService> _logger;
    private readonly IDockerService _dockerService;
    private readonly Dictionary<string, TerminalSession> _sessions = new();
    private readonly Dictionary<string, List<CommandEntry>> _histories = new();
    private readonly object _lock = new();

    public DockerTerminalService(
        IDockerService dockerService,
        ILogger<DockerTerminalService> logger)
    {
        _dockerService = dockerService;
        _logger = logger;
    }

    public async Task<TerminalSession> CreateSessionAsync(
        string workspaceId,
        ShellType shell = ShellType.Bash,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null,
        int rows = 24,
        int cols = 80,
        CancellationToken ct = default,
        string? sessionId = null)
    {
        sessionId ??= Guid.NewGuid().ToString();
        var cwd = workingDirectory ?? $"/workspace/{workspaceId}";
        var containerId = workspaceId; // Use workspaceId as containerId for now

        var session = new TerminalSession
        {
            Id = Guid.TryParse(sessionId, out var parsedId) ? parsedId : Guid.NewGuid(),
            UserId = Guid.Empty, // TODO: Get from user context
            Shell = shell,
            WorkingDirectory = cwd,
            EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>(),
            Status = SessionStatus.Active,
            Rows = rows,
            Cols = cols,
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow,
        };

        lock (_lock)
        {
            _sessions[sessionId] = session;
            _histories[sessionId] = new List<CommandEntry>();
        }

        // Initialize shell in Docker container
        try
        {
            await _dockerService.CreateShellSessionAsync(containerId, sessionId, shell, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shell session for workspace {WorkspaceId}", workspaceId);
            // Continue anyway - session is created but shell may not be initialized
        }

        _logger.LogInformation(
            "Created terminal session {SessionId} for workspace {WorkspaceId} with shell {Shell}",
            sessionId,
            workspaceId,
            shell);

        return session;
    }

    public Task<TerminalSession?> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_sessions.TryGetValue(sessionId, out var session) ? session : null);
        }
    }

    public Task<TerminalSession[]> ListSessionsAsync(string? workspaceId = null, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(workspaceId))
                return Task.FromResult(_sessions.Values.ToArray());
            
            return Task.FromResult(_sessions.Values
                .Where(s => s.WorkingDirectory.Contains(workspaceId))
                .ToArray());
        }
    }

    public async Task<CommandEntry> ExecuteCommandAsync(
        string sessionId,
        string command,
        string? workingDirectory = null,
        CancellationToken ct = default)
    {
        TerminalSession? session;
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out session))
                throw new KeyNotFoundException($"Session {sessionId} not found");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        string output = string.Empty;
        int exitCode = 0;
        string containerId = session.WorkingDirectory.Split('/').Last(); // Extract workspace ID as container ID

        try
        {
            // Execute command in Docker container
            output = await _dockerService.ExecuteCommandAsync(containerId, command.Split(' '), workingDirectory, ct);
            exitCode = 0; // TODO: Parse exit code from output
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command execution failed for session {SessionId}", sessionId);
            output = $"Error: {ex.Message}";
            exitCode = 1;
        }

        stopwatch.Stop();

        var entry = new CommandEntry
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.Parse(sessionId),
            Command = command,
            Output = output,
            ExitCode = exitCode,
            DurationMs = (int)stopwatch.ElapsedMilliseconds,
            ExecutedAt = DateTimeOffset.UtcNow,
        };

        lock (_lock)
        {
            if (_histories.TryGetValue(sessionId, out var history))
            {
                history.Add(entry);
            }
            session.LastActivityAt = DateTimeOffset.UtcNow;
        }

        _logger.LogInformation(
            "Executed command in session {SessionId}: {Command} (exit: {ExitCode}, duration: {DurationMs}ms)",
            sessionId,
            command,
            exitCode,
            entry.DurationMs);

        return entry;
    }

    public Task<CommandEntry[]> GetHistoryAsync(string sessionId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_histories.TryGetValue(sessionId, out var history)
                ? history.ToArray()
                : Array.Empty<CommandEntry>());
        }
    }

    public async Task TerminateSessionAsync(string sessionId, CancellationToken ct = default)
    {
        TerminalSession? session;
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out session))
                return;

            session.Status = SessionStatus.Terminated;
            session.TerminatedAt = DateTimeOffset.UtcNow;
        }

        var containerId = session.WorkingDirectory.Split('/').Last();
        
        try
        {
            await _dockerService.TerminateShellSessionAsync(containerId, sessionId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate shell session {SessionId}", sessionId);
        }

        _logger.LogInformation("Terminated terminal session {SessionId}", sessionId);
    }

    public Task ResizeAsync(string sessionId, int rows, int cols, CancellationToken ct = default)
    {
        TerminalSession? session;
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out session))
                throw new KeyNotFoundException($"Session {sessionId} not found");

            session.Rows = rows;
            session.Cols = cols;
        }

        // TODO: Send resize signal to shell process
        _logger.LogInformation(
            "Resized terminal session {SessionId} to {Rows}x{Cols}",
            sessionId,
            rows,
            cols);

        return Task.CompletedTask;
    }

    public async Task<string> GetOutputAsync(string sessionId, CancellationToken ct = default)
    {
        TerminalSession? session;
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out session))
                return string.Empty;
        }

        var containerId = session.WorkingDirectory.Split('/').Last();
        
        try
        {
            return await _dockerService.GetShellOutputAsync(containerId, sessionId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shell output for session {SessionId}", sessionId);
            return string.Empty;
        }
    }
}
