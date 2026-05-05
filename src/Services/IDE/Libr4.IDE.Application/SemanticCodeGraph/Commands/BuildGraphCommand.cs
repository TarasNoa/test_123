using MediatR;
using Libr4.IDE.Application.SemanticCodeGraph.DTOs;

namespace Libr4.IDE.Application.SemanticCodeGraph.Commands;

/// <summary>
/// Command to build a semantic graph
/// </summary>
public record BuildGraphCommand : IRequest<SemanticGraphDto>
{
    public string WorkspaceId { get; init; } = string.Empty;
    public List<(string FilePath, string Content)> Files { get; init; } = new();
}
