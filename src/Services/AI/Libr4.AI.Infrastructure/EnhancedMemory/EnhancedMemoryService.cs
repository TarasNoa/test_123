using Libr4.AI.Domain.Memory.Enhanced.FSharp;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.EnhancedMemory;

public class EnhancedMemoryService
{
    private readonly ILogger<EnhancedMemoryService> _logger;

    public EnhancedMemoryService(ILogger<EnhancedMemoryService> logger)
    {
        _logger = logger;
    }

    public Guid CreateMemory(
        MemoryLevel level,
        string content,
        string? userId = null,
        string? sessionId = null,
        Guid? agentId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var memory = MemoryOps.createMemory(level, content, now);
        
        // Set optional fields
        if (userId != null)
        {
            memory = memory with { userId = Some(userId) };
        }
        if (sessionId != null)
        {
            memory = memory with { sessionId = Some(sessionId) };
        }
        if (agentId != null)
        {
            memory = memory with { agentId = Some(agentId.Value) };
        }
        
        MemoryStoreOps.addMemory(memory);
        _logger.LogInformation("Created memory {MemoryId} with level {Level}", memory.id, level);
        
        return memory.id;
    }

    public EnhancedMemory? GetMemory(Guid id)
    {
        return MemoryStoreOps.getMemory(id);
    }

    public void UpdateMemoryEmbedding(Guid memoryId, float[] embedding)
    {
        var memory = MemoryStoreOps.getMemory(memoryId);
        if (memory != null)
        {
            var updated = MemoryOps.updateMemoryEmbedding(embedding, memory.Value);
            MemoryStoreOps.updateMemory(updated);
            _logger.LogDebug("Updated embedding for memory {MemoryId}", memoryId);
        }
    }

    public void AccessMemory(Guid memoryId)
    {
        var memory = MemoryStoreOps.getMemory(memoryId);
        if (memory != null)
        {
            var updated = MemoryOps.accessMemory(DateTimeOffset.UtcNow, memory.Value);
            MemoryStoreOps.updateMemory(updated);
        }
    }

    public void ConsolidateMemories(List<Guid> memoryIds)
    {
        var memories = memoryIds
            .Select(id => MemoryStoreOps.getMemory(id))
            .Where(m => m != null)
            .Select(m => m.Value)
            .ToList();

        if (memories.Count == 0)
        {
            _logger.LogWarning("No valid memories to consolidate");
            return;
        }

        var consolidated = MemoryOps.consolidateMemories(memories, DateTimeOffset.UtcNow);
        MemoryStoreOps.addMemory(consolidated);

        // Mark original memories for deletion (or soft delete)
        foreach (var memory in memories)
        {
            MemoryStoreOps.deleteMemory(memory.id);
        }

        _logger.LogInformation("Consolidated {Count} memories into {ConsolidatedId}", memories.Count, consolidated.id);
    }

    public List<MemorySearchResult> Search(
        string query,
        float[]? queryEmbedding = null,
        MemoryLevel? level = null,
        string? userId = null,
        string? sessionId = null,
        Guid? agentId = null,
        int topK = 10,
        float? threshold = null)
    {
        var fsharpQuery = new MemoryQuery(
            query: query,
            queryEmbedding: Option<float[]>.FromNullable(queryEmbedding),
            level: Option<MemoryLevel>.FromNullable(level),
            userId: Option<string>.FromNullable(userId),
            sessionId: Option<string>.FromNullable(sessionId),
            agentId: Option<Guid>.FromNullable(agentId),
            topK: topK,
            threshold: Option<float>.FromNullable(threshold)
        );

        var results = MemoryStoreOps.search(fsharpQuery);
        _logger.LogInformation("Search returned {Count} results for query: {Query}", results.Count, query);

        return results;
    }

    public List<EnhancedMemory> GetAllMemories()
    {
        return MemoryStoreOps.getAllMemories();
    }

    public void CleanupExpiredMemories()
    {
        var now = DateTimeOffset.UtcNow;
        var allMemories = MemoryStoreOps.getAllMemories();
        var expired = allMemories.Where(m => MemoryOps.shouldForget(m, now)).ToList();

        foreach (var memory in expired)
        {
            MemoryStoreOps.deleteMemory(memory.id);
        }

        _logger.LogInformation("Cleaned up {Count} expired memories", expired.Count);
    }
}
