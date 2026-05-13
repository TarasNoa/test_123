using MediatR;
using Libr4.IDE.Application.ShadowWorkspace.DTOs;

namespace Libr4.IDE.Application.ShadowWorkspace.Commands;

/// <summary>
/// Command to create a shadow workspace
/// </summary>
public record CreateShadowWorkspaceCommand : IRequest<ShadowWorkspaceDto>
{
    public string ParentWorkspaceId { get; init; } = string.Empty;
    public List<ShadowFileRequest> Files { get; init; } = new();
}

public record ShadowFileRequest(string FilePath, string Content);
