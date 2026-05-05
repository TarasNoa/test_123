using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Libr4.IDE.Infrastructure.Collaboration;

/// <summary>
/// CRDT document management for real-time collaborative editing
/// Uses Yjs-compatible protocol (Automerge in C#)
/// </summary>
public interface ICrdtDocumentService
{
    Task<CrdtDocument> CreateDocumentAsync(string workspaceId, string filePath);
    Task<CrdtDocument> GetDocumentAsync(string documentId);
    Task ApplyUpdateAsync(string documentId, byte[] update);
    Task<string> GetCurrentContentAsync(string documentId);
    Task<List<DocumentChange>> GetChangesAsync(string documentId, int sinceSequence = 0);
}

public class CrdtDocumentService : ICrdtDocumentService
{
    private readonly ILogger<CrdtDocumentService> _logger;
    private readonly Dictionary<string, CrdtDocument> _documents = new();
    private readonly Dictionary<string, List<DocumentChange>> _changeHistory = new();
    private int _sequenceCounter = 0;

    public CrdtDocumentService(ILogger<CrdtDocumentService> logger)
    {
        _logger = logger;
    }

    public Task<CrdtDocument> CreateDocumentAsync(string workspaceId, string filePath)
    {
        var documentId = $"{workspaceId}:{filePath}";
        var document = new CrdtDocument
        {
            Id = documentId,
            WorkspaceId = workspaceId,
            FilePath = filePath,
            CreatedAt = DateTime.UtcNow,
            State = new AutomergeState(),
            Sequence = 0
        };

        _documents[documentId] = document;
        _changeHistory[documentId] = new List<DocumentChange>();

        _logger.LogInformation("Created CRDT document {DocumentId} for {FilePath}", documentId, filePath);
        return Task.FromResult(document);
    }

    public Task<CrdtDocument> GetDocumentAsync(string documentId)
    {
        _documents.TryGetValue(documentId, out var document);
        return Task.FromResult(document!);
    }

    public Task ApplyUpdateAsync(string documentId, byte[] update)
    {
        if (!_documents.TryGetValue(documentId, out var document))
        {
            throw new InvalidOperationException($"Document {documentId} not found");
        }

        var sequence = Interlocked.Increment(ref _sequenceCounter);
        var change = new DocumentChange
        {
            Sequence = sequence,
            Timestamp = DateTime.UtcNow,
            Update = update,
            Author = "system"
        };

        _changeHistory[documentId].Add(change);
        document.Sequence = sequence;
        document.LastModified = DateTime.UtcNow;

        // Apply update to document state
        document.State.ApplyUpdate(update);

        _logger.LogDebug("Applied update to {DocumentId}, sequence {Sequence}", documentId, sequence);
        return Task.CompletedTask;
    }

    public Task<string> GetCurrentContentAsync(string documentId)
    {
        if (!_documents.TryGetValue(documentId, out var document))
        {
            return Task.FromResult(string.Empty);
        }

        var content = document.State.GetText();
        return Task.FromResult(content);
    }

    public Task<List<DocumentChange>> GetChangesAsync(string documentId, int sinceSequence = 0)
    {
        if (!_changeHistory.TryGetValue(documentId, out var history))
        {
            return Task.FromResult(new List<DocumentChange>());
        }

        var changes = history.Where(c => c.Sequence > sinceSequence).ToList();
        return Task.FromResult(changes);
    }
}

public class CrdtDocument
{
    public string Id { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public int Sequence { get; set; }
    public AutomergeState State { get; set; } = new();
}

public class DocumentChange
{
    public int Sequence { get; set; }
    public DateTime Timestamp { get; set; }
    public byte[] Update { get; set; } = Array.Empty<byte>();
    public string Author { get; set; } = string.Empty;
}

/// <summary>
/// Simplified Automerge state representation
/// In production, use actual Automerge library or port
/// </summary>
public class AutomergeState
{
    private string _text = string.Empty;
    private readonly List<byte[]> _updates = new();

    public void ApplyUpdate(byte[] update)
    {
        _updates.Add(update);
        // Simplified: In reality, decode Automerge binary format
        // For now, just append text representation
        _text += System.Text.Encoding.UTF8.GetString(update);
    }

    public string GetText()
    {
        return _text;
    }

    public byte[] Save()
    {
        // Simplified: Return concatenated updates
        return _updates.SelectMany(u => u).ToArray();
    }
}
