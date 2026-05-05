namespace Libr4.IDE.Application.SeniorRolePrompts.DTOs;

/// <summary>
/// DTO for RolePrompt
/// </summary>
public record RolePromptDto
{
    public Guid Id { get; init; }
    public string PhaseType { get; init; } = string.Empty;
    public string PhaseName { get; init; } = string.Empty;
    public SeniorRoleDto SeniorRole { get; init; } = null!;
    public string SystemPrompt { get; init; } = string.Empty;
    public string UserPrompt { get; init; } = string.Empty;
    public string DomainClass { get; init; } = string.Empty;
    public bool RichMode { get; init; }
    public DateTime GeneratedAt { get; init; }
}
