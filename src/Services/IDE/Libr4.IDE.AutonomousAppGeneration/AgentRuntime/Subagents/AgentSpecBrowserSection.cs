namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;

public sealed class AgentSpecBrowserSection
{
    public bool Enabled { get; set; } = true;

    public bool StealthMode { get; set; } = true;

    public string? DefaultUserAgent { get; set; }

    public List<int>? DefaultViewport { get; set; }

    public Dictionary<string, string> DefaultHeaders { get; set; } = new();

    public List<AgentSpecDataSelector> DataSelectors { get; set; } = new();

    public List<AgentSpecBrowserTask> Tasks { get; set; } = new();
}

public sealed class AgentSpecDataSelector
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Selector { get; set; } = string.Empty;

    public string Type { get; set; } = "css";

    public string? Attribute { get; set; }
}

public sealed class AgentSpecBrowserTask
{
    /// <summary>Alias for taskName (docs compatibility).</summary>
    public string Name { get; set; } = string.Empty;

    public string TaskName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? TimeoutSeconds { get; set; }

    public bool TakeScreenshots { get; set; } = true;

    public List<AgentSpecBrowserAction> Actions { get; set; } = new();

    public List<AgentSpecExtractionRule> ExtractionRules { get; set; } = new();
}

public sealed class AgentSpecBrowserAction
{
    public string Type { get; set; } = string.Empty;

    public string? Selector { get; set; }

    public string? Value { get; set; }

    public int? WaitMs { get; set; }
}

public sealed class AgentSpecExtractionRule
{
    public string FieldName { get; set; } = string.Empty;

    public string Type { get; set; } = "text";

    public string Selector { get; set; } = string.Empty;

    public string? Attribute { get; set; }

    public string? DefaultValue { get; set; }

    public bool Required { get; set; }
}
