namespace Libr4.IDE.Application.ArchitecturalGuardrails.DTOs;

/// <summary>
/// DTO for ArchitectureValidation
/// </summary>
public record ArchitectureValidationDto
{
    public Guid Id { get; init; }
    public string ValidationId { get; init; } = string.Empty;
    public string WorkspaceId { get; init; } = string.Empty;
    public List<GuardrailRuleDto> Rules { get; init; } = new();
    public List<GuardrailViolationDto> Violations { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
