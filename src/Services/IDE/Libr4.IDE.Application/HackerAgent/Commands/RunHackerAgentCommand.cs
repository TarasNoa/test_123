using MediatR;
using Libr4.IDE.Domain.HackerAgent;
using Libr4.IDE.Application.HackerAgent.DTOs;

namespace Libr4.IDE.Application.HackerAgent.Commands;

/// <summary>
/// Command to run hacker agent
/// </summary>
public record RunHackerAgentCommand : IRequest<HackerAgentDto>
{
    public string WorkspaceId { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public ScriptType ScriptType { get; init; } = ScriptType.Python;
}
