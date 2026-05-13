namespace Libr4.IDE.Application.Gateway;

/// <summary>
/// Interface for gateway preview integration
/// </summary>
public interface IGatewayPreviewIntegration
{
    Task<string> CreatePreviewAsync(string projectId, string[] filePaths, CancellationToken ct = default);
    Task<PreviewStatus> GetPreviewStatusAsync(string previewId, CancellationToken ct = default);
    Task<string?> GetPreviewUrlAsync(string previewId, CancellationToken ct = default);
    Task DestroyPreviewAsync(string previewId, CancellationToken ct = default);
}

public class PreviewStatus
{
    public string PreviewId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty; // "Creating", "Ready", "Error", "Destroyed"
    public string? Error { get; set; }
    public string? Summary { get; set; }
    public int FileCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
}
