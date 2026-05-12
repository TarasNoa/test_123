using Libr4.AI.Domain.Memory.Enhanced.FSharp;
using EnhancedMemoryEntity = Libr4.AI.Domain.Memory.Enhanced.FSharp.EnhancedMemory;
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
        // Apply optional fields if provided
        if (userId != null || sessionId != null || agentId != null)
        {
            memory = memory with {
                userId = userId != null ? Microsoft.FSharp.Core.FSharpOption<string>.Some(userId) : memory.userId,
                sessionId = sessionId != null ? Microsoft.FSharp.Core.FSharpOption<string>.Some(sessionId) : memory.sessionId,
                agentId = agentId.HasValue ? Microsoft.FSharp.Core.FSharpOption<Guid>.Some(agentId.Value) : memory.agentId
            };
        }
        MemoryStoreOps.addMemory(memory);
        _logger.LogInformation("Created memory {MemoryId} with level {Level}", memory.id, level);
        return memory.id;
    }

    public EnhancedMemoryEntity? GetMemory(Guid id)
    {
        return MemoryStoreOps.getMemory(id) is Microsoft.FSharp.Core.FSharpOption<EnhancedMemoryEntity>.Some m ? m : null;
    }

    public void AccessMemory(Guid id)
    {
        var now = DateTimeOffset.UtcNow;
        if (MemoryStoreOps.getMemory(id) is Microsoft.FSharp.Core.FSharpOption<EnhancedMemoryEntity>.Some memory)
        {
            var updated = MemoryOps.accessMemory(now, memory);
            MemoryStoreOps.updateMemory(updated);
        }
    }

    public void UpdateMemoryEmbedding(Guid id, float[] embedding)
    {
        if (MemoryStoreOps.getMemory(id) is Microsoft.FSharp.Core.FSharpOption<EnhancedMemoryEntity>.Some memory)
        {
            var updated = MemoryOps.updateMemoryEmbedding(embedding, memory);
            MemoryStoreOps.updateMemory(updated);
        }
    }

    public void ConsolidateMemories(List<Guid> memoryIds)
    {
        var now = DateTimeOffset.UtcNow;
        var memories = memoryIds
            .Select(id => MemoryStoreOps.getMemory(id))
            .Where(m => m is Microsoft.FSharp.Core.FSharpOption<EnhancedMemoryEntity>.Some)
            .Select(m => ((Microsoft.FSharp.Core.FSharpOption<EnhancedMemoryEntity>.Some)m).Value)
            .ToList();

        if (memories.Count > 0)
        {
            var consolidated = MemoryOps.consolidateMemories(memories, now);
            foreach (var id in memoryIds)
            {
                MemoryStoreOps.deleteMemory(id);
            }
            MemoryStoreOps.addMemory(consolidated);
        }
        _logger.LogInformation("Consolidated {Count} memories into {ConsolidatedId}", memories.Count, consolidated.id);
    }

    public List<EnhancedMemoryEntity> Search(
        string query,
        float[]? queryEmbedding = null,
        MemoryLevel? level = null,
        string? userId = null,
        string? sessionId = null,
        Guid? agentId = null,
        int topK = 10,
        float? threshold = null)
    {
        var memories = MemoryStoreOps.getAllMemories();
        var queryOpt = queryEmbedding != null ? Microsoft.FSharp.Core.FSharpOption<float[]>.Some(queryEmbedding) : Microsoft.FSharp.Core.FSharpOption<float[]>.None;
        var levelOpt = level.HasValue ? Microsoft.FSharp.Core.FSharpOption<MemoryLevel>.Some(level.Value) : Microsoft.FSharp.Core.FSharpOption<MemoryLevel>.None;
        var userOpt = userId != null ? Microsoft.FSharp.Core.FSharpOption<string>.Some(userId) : Microsoft.FSharp.Core.FSharpOption<string>.None;
        var sessionOpt = sessionId != null ? Microsoft.FSharp.Core.FSharpOption<string>.Some(sessionId) : Microsoft.FSharp.Core.FSharpOption<string>.None;
        var agentOpt = agentId.HasValue ? Microsoft.FSharp.Core.FSharpOption<Guid>.Some(agentId.Value) : Microsoft.FSharp.Core.FSharpOption<Guid>.None;

        var results = MemoryOps.filterByAgent(
            agentOpt,
            MemoryOps.filterBySession(
                sessionOpt,
                MemoryOps.filterByUser(
                    userOpt,
                    MemoryOps.filterByLevel(
                        levelOpt,
                        HybridSearchOps.hybridSearch(query, queryOpt, memories, topK)))));

        _logger.LogInformation("Search returned {Count} results for query: {Query}", results.Count, query);
        return results;
    }

    public List<EnhancedMemoryEntity> GetAllMemories()
    {
        return MemoryStoreOps.getAllMemories();
    }

    public void CleanupExpiredMemories()
    {
        var now = DateTimeOffset.UtcNow;
        var memories = MemoryStoreOps.getAllMemories();
        var expired = memories.Where(m => MemoryOps.shouldForget(m, now)).ToList();

        foreach (var memory in expired)
        {
            MemoryStoreOps.deleteMemory(memory.id);
        }
        _logger.LogInformation("Cleaned up {Count} expired memories", expired.Count);
    }
}
