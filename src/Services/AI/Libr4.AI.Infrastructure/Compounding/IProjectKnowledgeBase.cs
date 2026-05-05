namespace Libr4.AI.Infrastructure.Compounding;

/// <summary>
/// Project knowledge base for compounding engineering
/// Stores conventions, anti-patterns, and learned lessons that compound over time
/// Based on CLAUDE.md/.cursorrules pattern
/// </summary>
public interface IProjectKnowledgeBase
{
    /// <summary>
    /// Get knowledge for current project
    /// </summary>
    Task<string> GetProjectKnowledgeAsync(string projectPath);
    
    /// <summary>
    /// Add learned lesson to knowledge base
    /// </summary>
    Task AddLessonAsync(string projectPath, KnowledgeLesson lesson);
    
    /// <summary>
    /// Add convention to knowledge base
    /// </summary>
    Task AddConventionAsync(string projectPath, ProjectConvention convention);
    
    /// <summary>
    /// Add anti-pattern to knowledge base
    /// </summary>
    Task AddAntiPatternAsync(string projectPath, AntiPattern antiPattern);
    
    /// <summary>
    /// Get formatted knowledge as markdown for LLM context
    /// </summary>
    Task<string> GetFormattedKnowledgeAsync(string projectPath);
    
    /// <summary>
    /// Get design document (DESIGN.md)
    /// </summary>
    Task<string?> GetDesignAsync(string projectPath);
    
    /// <summary>
    /// Set design document (DESIGN.md)
    /// </summary>
    Task SetDesignAsync(string projectPath, string content);
}

public class KnowledgeLesson
{
    public string Title { get; set; } = string.Empty;
    public string Problem { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public string? CodeExample { get; set; }
    public DateTimeOffset LearnedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class ProjectConvention
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ConventionType Type { get; set; }
    public string? Example { get; set; }
    public bool IsRequired { get; set; }
}

public enum ConventionType
{
    Naming,
    Architecture,
    CodeStyle,
    Testing,
    Documentation
}

public class AntiPattern
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WhyAvoid { get; set; } = string.Empty;
    public string? Alternative { get; set; }
    public List<string> Contexts { get; set; } = new();
}
