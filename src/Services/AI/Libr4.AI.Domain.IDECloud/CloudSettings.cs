using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.IDECloud;

public class CloudSettings
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Theme { get; set; } = "dark";
    public string FontFamily { get; set; } = "Fira Code";
    public int FontSize { get; set; } = 14;
    public bool AutoSave { get; set; } = true;
    public int AutoSaveIntervalMs { get; set; } = 5000;
    public List<string> Extensions { get; set; } = new List<string>();
    public Dictionary<string, object> EditorSettings { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> KeyBindings { get; set; } = new Dictionary<string, string>();
    public DateTimeOffset LastSyncedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class Snippet
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class UserTheme
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Colors { get; set; } = new Dictionary<string, string>();
    public bool IsDark { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
