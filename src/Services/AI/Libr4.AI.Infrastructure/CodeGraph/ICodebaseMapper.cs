namespace Libr4.AI.Infrastructure.CodeGraph;

/// <summary>
/// Codebase Mapper - maps entire codebase for better context
/// Based on Aider pattern
/// </summary>
public interface ICodebaseMapper
{
    /// <summary>
    /// Generate codebase map
    /// </summary>
    Task<CodebaseMap> GenerateMapAsync(string projectPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get formatted map for LLM context
    /// </summary>
    Task<string> GetFormattedMapAsync(string projectPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get map for specific files/directories
    /// </summary>
    Task<CodebaseMap> GetPartialMapAsync(string projectPath, List<string> paths, CancellationToken cancellationToken = default);
}

public class CodebaseMap
{
    public string ProjectPath { get; set; } = string.Empty;
    public List<CodeFile> Files { get; set; } = new();
    public List<CodeDirectory> Directories { get; set; } = new();
    public Dictionary<string, List<string>> Dependencies { get; set; } = new();
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class CodeFile
{
    public string Path { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public List<string> Imports { get; set; } = new();
    public List<string> Exports { get; set; } = new();
    public string? Summary { get; set; }
}

public class CodeDirectory
{
    public string Path { get; set; } = string.Empty;
    public List<string> Files { get; set; } = new();
    public string? Purpose { get; set; }
}
