using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.Gateway;

/// <summary>
/// Stub implementation of gateway preview integration
/// </summary>
public class GatewayPreviewIntegration : IGatewayPreviewIntegration
{
    private readonly ILogger<GatewayPreviewIntegration> _logger;
    private readonly Dictionary<string, PreviewStatus> _previews = new();

    public GatewayPreviewIntegration(ILogger<GatewayPreviewIntegration> logger)
    {
        _logger = logger;
    }

    public Task<string> CreatePreviewAsync(string projectId, string[] filePaths, CancellationToken ct = default)
    {
        var previewId = Guid.NewGuid().ToString("N");
        _previews[previewId] = new PreviewStatus
        {
            PreviewId = previewId,
            State = "Ready",
            CreatedAt = DateTime.UtcNow,
            ReadyAt = DateTime.UtcNow
        };
        
        _logger.LogInformation("Created preview {PreviewId} for project {ProjectId}", previewId, projectId);
        return Task.FromResult(previewId);
    }

    public Task<PreviewStatus> GetPreviewStatusAsync(string previewId, CancellationToken ct = default)
    {
        if (!_previews.TryGetValue(previewId, out var status))
        {
            throw new KeyNotFoundException($"Preview {previewId} not found");
        }
        return Task.FromResult(status);
    }

    public Task<string?> GetPreviewUrlAsync(string previewId, CancellationToken ct = default)
    {
        return Task.FromResult<string?>($"https://preview.libr4.dev/{previewId}");
    }

    public Task DestroyPreviewAsync(string previewId, CancellationToken ct = default)
    {
        if (_previews.TryGetValue(previewId, out var status))
        {
            status.State = "Destroyed";
            _logger.LogInformation("Destroyed preview {PreviewId}", previewId);
        }
        return Task.CompletedTask;
    }
}
