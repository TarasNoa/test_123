using Libr4.Collaboration.Domain;

namespace Libr4.Collaboration.Infrastructure.Repositories;

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly List<SharedDocument> _documents = new();

    public Task<SharedDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_documents.FirstOrDefault(d => d.Id == id));
    }

    public Task AddAsync(SharedDocument document, CancellationToken cancellationToken = default)
    {
        _documents.Add(document);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SharedDocument document, CancellationToken cancellationToken = default)
    {
        var index = _documents.FindIndex(d => d.Id == document.Id);
        if (index >= 0) _documents[index] = document;
        return Task.CompletedTask;
    }
}
