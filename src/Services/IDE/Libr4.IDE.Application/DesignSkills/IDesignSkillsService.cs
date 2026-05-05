namespace Libr4.IDE.Application.DesignSkills;

/// <summary>
/// Service for applying design skills to UI generation (TypeUI Design Skills style)
/// </summary>
public interface IDesignSkillsService
{
    /// <summary>
    /// Apply design skills to a UI component description
    /// </summary>
    Task<string> ApplyDesignSkillAsync(string componentDescription, string skillType, CancellationToken ct = default);
    
    /// <summary>
    /// Get available design skills
    /// </summary>
    Task<string[]> GetAvailableSkillsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Generate UI component with specific design skills applied
    /// </summary>
    Task<string> GenerateComponentWithSkillsAsync(string componentName, string[] skills, CancellationToken ct = default);
    
    /// <summary>
    /// Evaluate design quality based on skills
    /// </summary>
    Task<DesignEvaluation> EvaluateDesignAsync(string componentCode, CancellationToken ct = default);
}

/// <summary>
/// Design evaluation result
/// </summary>
public class DesignEvaluation
{
    public double OverallScore { get; init; }
    public string[] Strengths { get; init; } = Array.Empty<string>();
    public string[] Weaknesses { get; init; } = Array.Empty<string>();
    public string[] Suggestions { get; init; } = Array.Empty<string>();
}
