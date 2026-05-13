using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.Gateway;

/// <summary>
/// Gateway preview integration with AI-powered project summary generation.
/// </summary>
public class GatewayPreviewIntegration : IGatewayPreviewIntegration
{
    private readonly IAIService _aiService;
    private readonly ILogger<GatewayPreviewIntegration> _logger;
    private readonly Dictionary<string, PreviewStatus> _previews = new();

    public GatewayPreviewIntegration(
        IAIService aiService,
        ILogger<GatewayPreviewIntegration> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<string> CreatePreviewAsync(string projectId, string[] filePaths, CancellationToken ct = default)
    {
        var previewId = Guid.NewGuid().ToString("N");

        // Gather file contents for AI summary
        var fileContents = new List<string>();
        foreach (var path in filePaths.Where(File.Exists).Take(20))
        {
            try
            {
                var content = await File.ReadAllTextAsync(path, ct);
                fileContents.Add($"File: {Path.GetFileName(path)}\n{content[..Math.Min(content.Length, 2000)]}");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not read preview file {Path}", path);
            }
        }

        string? aiSummary = null;
        if (fileContents.Count > 0)
        {
            var prompt = $"Summarize this project ({projectId}) based on the following files:\n\n{string.Join("\n---\n", fileContents)}";
            aiSummary = await _aiService.GenerateCompletionAsync(prompt, "You are a technical project summarizer. Create a concise 2-sentence summary.", null);
        }

        _previews[previewId] = new PreviewStatus
        {
            PreviewId = previewId,
            ProjectId = projectId,
            State = "Ready",
            CreatedAt = DateTime.UtcNow,
            ReadyAt = DateTime.UtcNow,
            Summary = aiSummary,
            FileCount = filePaths.Length
        };
        
        _logger.LogInformation("Created preview {PreviewId} for project {ProjectId} with {FileCount} files", previewId, projectId, filePaths.Length);
        return previewId;
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
