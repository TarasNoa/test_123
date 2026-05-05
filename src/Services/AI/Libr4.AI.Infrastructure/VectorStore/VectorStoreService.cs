using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.VectorStore;

public class VectorStoreService
{
    private readonly VectorDbContext _context;
    private readonly ILogger<VectorStoreService> _logger;

    public VectorStoreService(VectorDbContext context, ILogger<VectorStoreService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddMemoryVectorAsync(
        string memoryId,
        Vector embedding,
        string userId,
        string? agentId = null,
        string? sessionId = null,
        Dictionary<string, string>? metadata = null)
    {
        var vector = new MemoryVector
        {
            Id = Guid.NewGuid(),
            MemoryId = memoryId,
            Embedding = embedding,
            UserId = userId,
            AgentId = agentId ?? string.Empty,
            SessionId = sessionId ?? string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        _context.MemoryVectors.Add(vector);
        await _context.SaveChangesAsync();
        
        _logger.LogDebug("Added vector for memory {MemoryId}", memoryId);
    }

    public async Task UpdateMemoryVectorAsync(
        string memoryId,
        Vector embedding,
        Dictionary<string, string>? metadata = null)
    {
        var vector = await _context.MemoryVectors
            .FirstOrDefaultAsync(v => v.MemoryId == memoryId);

        if (vector != null)
        {
            vector.Embedding = embedding;
            if (metadata != null)
            {
                vector.Metadata = metadata;
            }
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Updated vector for memory {MemoryId}", memoryId);
        }
    }

    public async Task<List<MemoryVector>> SearchSimilarAsync(
        Vector queryEmbedding,
        int topK = 10,
        string? userId = null,
        string? agentId = null,
        string? sessionId = null)
    {
        var query = _context.MemoryVectors.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(m => m.UserId == userId);
        }
        if (!string.IsNullOrEmpty(agentId))
        {
            query = query.Where(m => m.AgentId == agentId);
        }
        if (!string.IsNullOrEmpty(sessionId))
        {
            query = query.Where(m => m.SessionId == sessionId);
        }

        // Similarity search using cosine distance
        var results = await query
            .OrderByDescending(m => m.Embedding.CosineDistance(queryEmbedding))
            .Take(topK)
            .ToListAsync();

        _logger.LogInformation("Found {Count} similar vectors", results.Count);
        return results;
    }

    public async Task DeleteMemoryVectorAsync(string memoryId)
    {
        var vector = await _context.MemoryVectors
            .FirstOrDefaultAsync(v => v.MemoryId == memoryId);

        if (vector != null)
        {
            _context.MemoryVectors.Remove(vector);
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Deleted vector for memory {MemoryId}", memoryId);
        }
    }

    public async Task<MemoryVector?> GetMemoryVectorAsync(string memoryId)
    {
        return await _context.MemoryVectors
            .FirstOrDefaultAsync(v => v.MemoryId == memoryId);
    }

    public async Task CleanupExpiredVectorsAsync(DateTimeOffset cutoffDate)
    {
        var expired = await _context.MemoryVectors
            .Where(m => m.CreatedAt < cutoffDate)
            .ToListAsync();

        _context.MemoryVectors.RemoveRange(expired);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Cleaned up {Count} expired vectors", expired.Count);
    }
}
