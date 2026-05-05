using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.IDEDebug;

public enum BreakpointType { Line, Conditional, Functional, Exception }
public enum DebugSessionStatus { NotStarted, Running, Paused, Stopped }

public class Breakpoint
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public int? Column { get; set; }
    public BreakpointType Type { get; set; } = BreakpointType.Line;
    public string? Condition { get; set; }
    public int HitCount { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}

public class DebugSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public DebugSessionStatus Status { get; set; } = DebugSessionStatus.NotStarted;
    public List<Breakpoint> Breakpoints { get; set; } = new List<Breakpoint>();
    public List<StackFrame> CallStack { get; set; } = new List<StackFrame>();
    public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class StackFrame
{
    public int Index { get; set; }
    public string FunctionName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public Dictionary<string, object> LocalVariables { get; set; } = new Dictionary<string, object>();
}
