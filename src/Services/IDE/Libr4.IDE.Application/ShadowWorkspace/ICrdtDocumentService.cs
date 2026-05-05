namespace Libr4.IDE.Application.ShadowWorkspace;

/// <summary>
/// Interface for CRDT document service
/// </summary>
public interface ICrdtDocumentService
{
    Task<string> CreateDocumentAsync(string content, string ownerId, CancellationToken ct = default);
    Task<string?> GetDocumentAsync(string documentId, CancellationToken ct = default);
    Task<bool> UpdateDocumentAsync(string documentId, string newContent, string editorId, CancellationToken ct = default);
    Task<bool> DeleteDocumentAsync(string documentId, CancellationToken ct = default);
}
