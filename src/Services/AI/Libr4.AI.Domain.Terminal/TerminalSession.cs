using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.Terminal;

public enum ShellType { Bash, Zsh, Fish, PowerShell, Cmd }
public enum SessionStatus { Active, Idle, Terminated }

public class TerminalSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ShellType Shell { get; set; } = ShellType.Bash;
    public string WorkingDirectory { get; set; } = "/";
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public List<CommandEntry> History { get; set; } = new List<CommandEntry>();
    public int Rows { get; set; } = 24;
    public int Cols { get; set; } = 80;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastActivityAt { get; set; }
    public DateTimeOffset? TerminatedAt { get; set; }

    public void ExecuteCommand(string command, string output)
    {
        History.Add(new CommandEntry
        {
            Id = Guid.NewGuid(),
            SessionId = Id,
            Command = command,
            Output = output,
            ExecutedAt = DateTimeOffset.UtcNow
        });
        LastActivityAt = DateTimeOffset.UtcNow;
    }
}

public class CommandEntry
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Command { get; set; } = string.Empty;
    public string? Output { get; set; }
    public int? ExitCode { get; set; }
    public int DurationMs { get; set; }
    public DateTimeOffset ExecutedAt { get; set; }
}
