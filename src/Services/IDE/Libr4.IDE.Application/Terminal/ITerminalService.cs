using Libr4.AI.Domain.Terminal;

namespace Libr4.IDE.Application.Terminal;

public interface ITerminalService
{
    Task<TerminalSession> CreateSessionAsync(
        string workspaceId,
        ShellType shell = ShellType.Bash,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null,
        int rows = 24,
        int cols = 80,
        CancellationToken ct = default);

    Task<TerminalSession?> GetSessionAsync(string sessionId, CancellationToken ct = default);
    
    Task<TerminalSession[]> ListSessionsAsync(string? workspaceId = null, CancellationToken ct = default);
    
    Task<CommandEntry> ExecuteCommandAsync(
        string sessionId,
        string command,
        string? workingDirectory = null,
        CancellationToken ct = default);
    
    Task<CommandEntry[]> GetHistoryAsync(string sessionId, CancellationToken ct = default);
    
    Task TerminateSessionAsync(string sessionId, CancellationToken ct = default);
    
    Task ResizeAsync(string sessionId, int rows, int cols, CancellationToken ct = default);
    
    Task<string> GetOutputAsync(string sessionId, CancellationToken ct = default);
}
