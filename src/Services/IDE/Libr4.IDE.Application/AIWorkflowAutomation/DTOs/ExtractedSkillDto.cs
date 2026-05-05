namespace Libr4.IDE.Application.AIWorkflowAutomation.DTOs;

/// <summary>
/// DTO for ExtractedSkill
/// </summary>
public record ExtractedSkillDto
{
    public Guid Id { get; init; }
    public string SkillName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Capabilities { get; init; } = new();
    public double ConfidenceScore { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}
