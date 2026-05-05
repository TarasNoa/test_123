using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.IDERunner;

public enum RunStatus { Pending, Running, Completed, Failed, Timeout }
public enum RuntimeType { Node, Python, Dotnet, Java, Go, Rust, Ruby, Php, Docker }

public class RunConfig
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public List<string> Args { get; set; } = new List<string>();
    public RuntimeType Runtime { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
    public string WorkingDirectory { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 300;
    public Dictionary<string, object> ResourceLimits { get; set; } = new Dictionary<string, object>();
    public DateTimeOffset CreatedAt { get; set; }
}

public class RunResult
{
    public Guid Id { get; set; }
    public Guid ConfigId { get; set; }
    public RunStatus Status { get; set; } = RunStatus.Pending;
    public string? Stdout { get; set; }
    public string? Stderr { get; set; }
    public int? ExitCode { get; set; }
    public int? DurationMs { get; set; }
    public long? MemoryUsedBytes { get; set; }
    public float? CpuUsagePercent { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
