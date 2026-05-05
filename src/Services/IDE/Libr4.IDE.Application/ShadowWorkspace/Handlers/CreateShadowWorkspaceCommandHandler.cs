/*
using MediatR;
using Libr4.IDE.Application.ShadowWorkspace.Commands;
using Libr4.IDE.Application.ShadowWorkspace.DTOs;
using Libr4.AI.Infrastructure.AI;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.ShadowWorkspace.Handlers;

/// <summary>
/// Handler for CreateShadowWorkspaceCommand - Creates isolated workspace for experiments
/// </summary>
public class CreateShadowWorkspaceCommandHandler : IRequestHandler<CreateShadowWorkspaceCommand, ShadowWorkspaceDto>
{
    private readonly ILogger<CreateShadowWorkspaceCommandHandler> _logger;

    public CreateShadowWorkspaceCommandHandler(ILogger<CreateShadowWorkspaceCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ShadowWorkspaceDto> Handle(CreateShadowWorkspaceCommand request, CancellationToken ct)
    {
        var workspaceId = $"shadow-{Guid.NewGuid():N}";
        var workspacePath = Path.Combine(Path.GetTempPath(), "libr4-shadow", workspaceId);

        try
        {
            // Create directory
            Directory.CreateDirectory(workspacePath);

            // Copy files
            foreach (var (filePath, content) in request.Files)
            {
                var fullPath = Path.Combine(workspacePath, filePath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await File.WriteAllTextAsync(fullPath, content, ct);
            }

            _logger.LogInformation("Created shadow workspace {WorkspaceId} with {FileCount} files",
                workspaceId, request.Files.Count);

            return new ShadowWorkspaceDto
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Path = workspacePath,
                ParentWorkspaceId = request.ParentWorkspaceId,
                FileCount = request.Files.Count,
                CreatedAt = DateTime.UtcNow,
                Status = "Active"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shadow workspace");
            throw;
        }
    }
}
*/
