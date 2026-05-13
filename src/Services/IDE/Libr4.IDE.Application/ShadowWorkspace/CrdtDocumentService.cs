using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.ShadowWorkspace;

/// <summary>
/// CRDT document service with LWW (Last-Write-Wins) merge and edit history.
/// </summary>
public class CrdtDocumentService : ICrdtDocumentService
{
    private readonly ILogger<CrdtDocumentService> _logger;
    private readonly Dictionary<string, DocumentEntry> _documents = new();
    private readonly object _lock = new();

    public CrdtDocumentService(ILogger<CrdtDocumentService> logger)
    {
        _logger = logger;
    }

    public Task<string> CreateDocumentAsync(string content, string ownerId, CancellationToken ct = default)
    {
        var documentId = Guid.NewGuid().ToString("N");
        lock (_lock)
        {
            _documents[documentId] = new DocumentEntry
            {
                DocumentId = documentId,
                Content = content,
                OwnerId = ownerId,
                Version = 1,
                LastModifiedAt = DateTime.UtcNow,
                EditHistory = new List<EditOperation>
                {
                    new() { EditorId = ownerId, Timestamp = DateTime.UtcNow, Version = 1, Delta = content }
                }
            };
        }
        
        _logger.LogInformation("Created document {DocumentId} for owner {OwnerId}", documentId, ownerId);
        return Task.FromResult(documentId);
    }

    public Task<string?> GetDocumentAsync(string documentId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_documents.TryGetValue(documentId, out var doc))
            {
                return Task.FromResult<string?>(doc.Content);
            }
        }
        return Task.FromResult<string?>(null);
    }

    public Task<bool> UpdateDocumentAsync(string documentId, string newContent, string editorId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (!_documents.TryGetValue(documentId, out var doc))
            {
                return Task.FromResult(false);
            }

            var timestamp = DateTime.UtcNow;
            var version = doc.Version + 1;

            doc.Content = newContent;
            doc.Version = version;
            doc.LastModifiedAt = timestamp;
            doc.EditHistory.Add(new EditOperation
            {
                EditorId = editorId,
                Timestamp = timestamp,
                Version = version,
                Delta = ComputeDelta(doc.EditHistory[^2]?.Delta ?? "", newContent)
            });
        }
        
        _logger.LogInformation("Updated document {DocumentId} by {EditorId} to version {Version}", 
            documentId, editorId, _documents[documentId].Version);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteDocumentAsync(string documentId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_documents.Remove(documentId))
            {
                _logger.LogInformation("Deleted document {DocumentId}", documentId);
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }

    public Task<string?> MergeDocumentAsync(string documentId, string remoteContent, string remoteEditorId, DateTime remoteTimestamp, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (!_documents.TryGetValue(documentId, out var doc))
            {
                return Task.FromResult<string?>(null);
            }

            // LWW (Last-Write-Wins) CRDT merge
            if (remoteTimestamp > doc.LastModifiedAt)
            {
                var oldContent = doc.Content;
                doc.Content = remoteContent;
                doc.Version++;
                doc.LastModifiedAt = remoteTimestamp;
                doc.EditHistory.Add(new EditOperation
                {
                    EditorId = remoteEditorId,
                    Timestamp = remoteTimestamp,
                    Version = doc.Version,
                    Delta = ComputeDelta(oldContent, remoteContent)
                });
                _logger.LogInformation("Merged remote changes into document {DocumentId} from {EditorId}", documentId, remoteEditorId);
            }
            else
            {
                _logger.LogInformation("Local version is newer for document {DocumentId}; remote changes discarded", documentId);
            }

            return Task.FromResult<string?>(doc.Content);
        }
    }

    public Task<IReadOnlyList<EditOperation>> GetEditHistoryAsync(string documentId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_documents.TryGetValue(documentId, out var doc))
            {
                return Task.FromResult<IReadOnlyList<EditOperation>>(doc.EditHistory.AsReadOnly());
            }
        }
        return Task.FromResult<IReadOnlyList<EditOperation>>(Array.Empty<EditOperation>());
    }

    public Task ApplyUpdateAsync(string documentId, byte[] update, CancellationToken ct = default)
    {
        var newContent = System.Text.Encoding.UTF8.GetString(update);
        lock (_lock)
        {
            if (!_documents.TryGetValue(documentId, out var doc))
            {
                _documents[documentId] = new DocumentEntry
                {
                    DocumentId = documentId,
                    Content = newContent,
                    OwnerId = "system",
                    Version = 1,
                    LastModifiedAt = DateTime.UtcNow,
                    EditHistory = new List<EditOperation>()
                };
            }
            else
            {
                doc.Version++;
                doc.Content = newContent;
                doc.LastModifiedAt = DateTime.UtcNow;
                doc.EditHistory.Add(new EditOperation
                {
                    EditorId = "system",
                    Timestamp = DateTime.UtcNow,
                    Version = doc.Version,
                    Delta = ComputeDelta(doc.Content, newContent)
                });
            }
        }
        return Task.CompletedTask;
    }

    private static string ComputeDelta(string oldContent, string newContent)
    {
        if (string.IsNullOrEmpty(oldContent)) return newContent;
        // Simple diff representation: if lengths are similar, store only changed suffix
        var commonLength = 0;
        var minLen = Math.Min(oldContent.Length, newContent.Length);
        while (commonLength < minLen && oldContent[commonLength] == newContent[commonLength])
            commonLength++;
        return $"[+{commonLength}:{newContent[commonLength..]}]";
    }

    private class DocumentEntry
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateTime LastModifiedAt { get; set; }
        public List<EditOperation> EditHistory { get; set; } = new();
    }
}

public class EditOperation
{
    public string EditorId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Version { get; set; }
    public string Delta { get; set; } = string.Empty;
}
