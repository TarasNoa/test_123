using MediatR;
using Libr4.IDE.Application.ShadowWorkspace.DTOs;

namespace Libr4.IDE.Application.ShadowWorkspace.Commands;

/// <summary>
/// Command to create a shadow workspace
/// </summary>
public record CreateShadowWorkspaceCommand : IRequest<ShadowWorkspaceDto>
{
    public string ParentWorkspaceId { get; init; } = string.Empty;
    public List<(string FilePath, string Content)> Files { get; init; } = new();
}
