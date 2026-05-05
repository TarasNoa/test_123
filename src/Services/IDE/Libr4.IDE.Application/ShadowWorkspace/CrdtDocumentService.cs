using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.ShadowWorkspace;

/// <summary>
/// Stub implementation of CRDT document service
/// </summary>
public class CrdtDocumentService : ICrdtDocumentService
{
    private readonly ILogger<CrdtDocumentService> _logger;
    private readonly Dictionary<string, DocumentEntry> _documents = new();

    public CrdtDocumentService(ILogger<CrdtDocumentService> logger)
    {
        _logger = logger;
    }

    public Task<string> CreateDocumentAsync(string content, string ownerId, CancellationToken ct = default)
    {
        var documentId = Guid.NewGuid().ToString("N");
        _documents[documentId] = new DocumentEntry
        {
            DocumentId = documentId,
            Content = content,
            OwnerId = ownerId,
            Version = 1,
            LastModifiedAt = DateTime.UtcNow
        };
        
        _logger.LogInformation("Created document {DocumentId} for owner {OwnerId}", documentId, ownerId);
        return Task.FromResult(documentId);
    }

    public Task<string?> GetDocumentAsync(string documentId, CancellationToken ct = default)
    {
        if (_documents.TryGetValue(documentId, out var doc))
        {
            return Task.FromResult<string?>(doc.Content);
        }
        return Task.FromResult<string?>(null);
    }

    public Task<bool> UpdateDocumentAsync(string documentId, string newContent, string editorId, CancellationToken ct = default)
    {
        if (!_documents.TryGetValue(documentId, out var doc))
        {
            return Task.FromResult(false);
        }

        doc.Content = newContent;
        doc.Version++;
        doc.LastModifiedAt = DateTime.UtcNow;
        
        _logger.LogInformation("Updated document {DocumentId} by {EditorId} to version {Version}", 
            documentId, editorId, doc.Version);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteDocumentAsync(string documentId, CancellationToken ct = default)
    {
        if (_documents.Remove(documentId))
        {
            _logger.LogInformation("Deleted document {DocumentId}", documentId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    private class DocumentEntry
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateTime LastModifiedAt { get; set; }
    }
}
