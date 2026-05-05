namespace Libr4.AI.Domain.Agents;

public enum SubagentCategory
{
    CoreDevelopment,
    LanguageSpecialist,
    Infrastructure,
    DataAndAI,
    MetaAndOrchestration,
    ResearchAndAnalysis,
    Architecture,
    Mobile,
    Web,
    Domain
}

public enum ModelTier
{
    Haiku,  // Fast, cheap
    Sonnet, // Balanced
    Opus    // Most capable
}

public class SubagentDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SubagentCategory Category { get; set; }
    public ModelTier DefaultModel { get; set; }
    public List<string> AssignedTools { get; set; } = new();
    public string SystemPrompt { get; set; } = string.Empty;
    public Dictionary<string, object> Capabilities { get; set; } = new();
}

public class SubagentInstance
{
    public Guid Id { get; set; }
    public string SubagentId { get; set; } = string.Empty;
    public Guid ParentAgentId { get; set; }
    public ModelTier CurrentModel { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public int UsageCount { get; set; }
}
