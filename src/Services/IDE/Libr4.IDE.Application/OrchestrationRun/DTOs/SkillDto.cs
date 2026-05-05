namespace Libr4.IDE.Application.OrchestrationRun.DTOs;

/// <summary>
/// DTO for Skill
/// </summary>
public record SkillDto
{
    public string SkillType { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Capabilities { get; init; } = new();
    public Dictionary<string, object> Requirements { get; init; } = new();
    public bool IsDefault { get; init; }
}
