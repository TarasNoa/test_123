using MediatR;
using Libr4.IDE.Application.AutonomousRuntimePolicy.DTOs;

namespace Libr4.IDE.Application.AutonomousRuntimePolicy.Commands;

/// <summary>
/// Command to generate a runtime policy
/// </summary>
public record GeneratePolicyCommand : IRequest<RuntimePolicyDto>
{
    public string Prompt { get; init; } = string.Empty;
    public string WorkspaceId { get; init; } = string.Empty;
}
