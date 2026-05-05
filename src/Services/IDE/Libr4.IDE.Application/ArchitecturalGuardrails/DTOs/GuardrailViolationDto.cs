namespace Libr4.IDE.Application.ArchitecturalGuardrails.DTOs;

/// <summary>
/// DTO for GuardrailViolation
/// </summary>
public record GuardrailViolationDto
{
    public Guid Id { get; init; }
    public GuardrailRuleDto Rule { get; init; } = null!;
    public string FilePath { get; init; } = string.Empty;
    public int LineNumber { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
}
