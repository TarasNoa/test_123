using MediatR;
using Libr4.IDE.Domain.SeniorRolePrompts;
using Libr4.IDE.Application.SeniorRolePrompts.DTOs;

namespace Libr4.IDE.Application.SeniorRolePrompts.Commands;

/// <summary>
/// Command to generate a role prompt for a specific phase
/// </summary>
public record GenerateRolePromptCommand : IRequest<RolePromptDto>
{
    public PhaseType PhaseType { get; init; }
    public string PhaseName { get; init; } = string.Empty;
    public string DomainClass { get; init; } = "Standard";
    public bool RichMode { get; init; } = false;
}
