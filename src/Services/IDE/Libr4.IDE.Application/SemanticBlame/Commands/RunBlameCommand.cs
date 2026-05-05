using MediatR;
using Libr4.IDE.Application.SemanticBlame.DTOs;

namespace Libr4.IDE.Application.SemanticBlame.Commands;

/// <summary>
/// Command to run semantic blame
/// </summary>
public record RunBlameCommand : IRequest<SemanticBlameDto>
{
    public string FilePath { get; init; } = string.Empty;
    public string WorkspacePath { get; init; } = string.Empty;
}
