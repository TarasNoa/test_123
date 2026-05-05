namespace Libr4.IDE.Application.ArchitecturalGuardrails.DTOs;

/// <summary>
/// DTO for GuardrailRule
/// </summary>
public record GuardrailRuleDto
{
    public Guid Id { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Pattern { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
