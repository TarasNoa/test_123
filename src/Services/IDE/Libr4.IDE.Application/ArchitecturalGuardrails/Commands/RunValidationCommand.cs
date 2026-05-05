using MediatR;
using Libr4.IDE.Domain.ArchitecturalGuardrails;
using Libr4.IDE.Application.ArchitecturalGuardrails.DTOs;

namespace Libr4.IDE.Application.ArchitecturalGuardrails.Commands;

/// <summary>
/// Command to run architecture validation
/// </summary>
public record RunValidationCommand : IRequest<ArchitectureValidationDto>
{
    public string WorkspaceId { get; init; } = string.Empty;
    public List<(string FilePath, string Content)> Files { get; init; } = new();
    public List<GuardrailRule> CustomRules { get; init; } = new();
}
