namespace Libr4.IDE.Application.DesignContext;

/// <summary>
/// Service for managing DESIGN.md context for UI generation (awesome-design-md style)
/// </summary>
public interface IDesignContextService
{
    /// <summary>
    /// Get design context for a specific workspace
    /// </summary>
    Task<DesignContext?> GetDesignContextAsync(string workspacePath, CancellationToken ct = default);
    
    /// <summary>
    /// Create or update DESIGN.md for a workspace
    /// </summary>
    Task SaveDesignContextAsync(string workspacePath, DesignContext context, CancellationToken ct = default);
    
    /// <summary>
    /// Generate a design context based on existing codebase
    /// </summary>
    Task<DesignContext> GenerateDesignContextAsync(string workspacePath, CancellationToken ct = default);
    
    /// <summary>
    /// Get UI generation prompt with design context
    /// </summary>
    Task<string> GetUIPromptAsync(string workspacePath, string task, CancellationToken ct = default);
}

/// <summary>
/// Design context for UI generation
/// </summary>
public class DesignContext
{
    public string ProjectName { get; set; } = string.Empty;
    public string DesignSystem { get; set; } = string.Empty;
    public string ColorPalette { get; set; } = string.Empty;
    public string Typography { get; set; } = string.Empty;
    public string ComponentLibrary { get; set; } = string.Empty;
    public string SpacingScale { get; set; } = string.Empty;
    public string Breakpoints { get; set; } = string.Empty;
    public string[] DesignPrinciples { get; set; } = Array.Empty<string>();
    public string[] ComponentPatterns { get; set; } = Array.Empty<string>();
}
