using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.IDELSP;

public enum LSPServerStatus { Starting, Running, Stopped, Error }

public class LSPServer
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public LSPServerStatus Status { get; set; } = LSPServerStatus.Stopped;
    public Dictionary<string, object> Capabilities { get; set; } = new Dictionary<string, object>();
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CompletionRequest
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Character { get; set; }
    public List<CompletionItem> Items { get; set; } = new List<CompletionItem>();
    public DateTimeOffset CreatedAt { get; set; }
}

public class CompletionItem
{
    public string Label { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? Documentation { get; set; }
    public int Kind { get; set; }
    public string? InsertText { get; set; }
}
