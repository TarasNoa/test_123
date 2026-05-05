using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Memory;

/// <summary>
/// Implementation of enhanced memory with Hebbian graph
/// </summary>
public class EnhancedMemoryWithGraph : IEnhancedMemoryWithGraph
{
    private readonly IEnhancedMemory _baseMemory;
    private readonly HebbianMemoryGraph _graph;
    private readonly ILogger<EnhancedMemoryWithGraph> _logger;
    private readonly Dictionary<string, HebbianMemoryGraph> _userGraphs = new();

    public EnhancedMemoryWithGraph(
        IEnhancedMemory baseMemory,
        ILogger<EnhancedMemoryWithGraph> logger)
    {
        _baseMemory = baseMemory;
        _graph = new HebbianMemoryGraph();
        _logger = logger;
    }

    public async Task AddMemoryAsync(string userId, string content, Dictionary<string, string>? metadata = null)
    {
        await _baseMemory.AddMemoryAsync(userId, content, metadata);
        
        // Extract concepts and add to graph
        var concepts = ExtractConcepts(content);
        var userGraph = GetUserGraph(userId);
        userGraph.AddMemory(content, concepts);
        
        _logger.LogDebug("Added memory with {Count} concepts for user {UserId}", concepts.Count, userId);
    }

    public async Task AddMemoryWithConceptsAsync(string userId, string content, List<string> concepts)
    {
        await _baseMemory.AddMemoryAsync(userId, content);
        
        var userGraph = GetUserGraph(userId);
        userGraph.AddMemory(content, concepts);
        
        _logger.LogDebug("Added memory with {Count} explicit concepts for user {UserId}", concepts.Count, userId);
    }

    public async Task<List<MemoryItem>> RetrieveAsync(string userId, string query, int topK = 5)
    {
        return await _baseMemory.RetrieveAsync(userId, query, topK);
    }

    public async Task<List<MemoryItem>> RetrieveWithBoostAsync(string userId, string query, int topK = 5)
    {
        // Get base results
        var baseResults = await _baseMemory.RetrieveAsync(userId, query, topK);
        
        // Get Hebbian boost
        var userGraph = GetUserGraph(userId);
        var queryConcepts = ExtractConcepts(query);
        var boostedConcepts = new HashSet<string>();
        
        foreach (var concept in queryConcepts)
        {
            var associations = userGraph.GetAssociatedConcepts(concept, 3);
            foreach (var assoc in associations.Where(a => a.Strength > 0.3f))
            {
                boostedConcepts.Add(assoc.Concept);
            }
        }
        
        // If we have boosted concepts, do additional retrieval
        if (boostedConcepts.Any())
        {
            var boostedQuery = string.Join(" ", queryConcepts.Concat(boostedConcepts));
            var boostedResults = await _baseMemory.RetrieveAsync(userId, boostedQuery, topK);
            
            // Merge and deduplicate
            var allResults = baseResults.Concat(boostedResults)
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .OrderByDescending(r => r.Similarity)
                .Take(topK)
                .ToList();
            
            _logger.LogDebug("Retrieved with Hebbian boost: {BaseCount} -> {BoostedCount}", 
                baseResults.Count, allResults.Count);
            
            return allResults;
        }
        
        return baseResults;
    }

    public async Task<List<string>> GetAssociatedConceptsAsync(string userId, string query)
    {
        var userGraph = GetUserGraph(userId);
        var queryConcepts = ExtractConcepts(query);
        var allAssociations = new List<string>();
        
        foreach (var concept in queryConcepts)
        {
            var associations = userGraph.GetAssociatedConcepts(concept, 5);
            allAssociations.AddRange(associations.Select(a => a.Concept));
        }
        
        return allAssociations.Distinct().ToList();
    }

    public async Task<Dictionary<string, ConsolidationStatus>> GetConsolidationStatusAsync(string userId)
    {
        var userGraph = GetUserGraph(userId);
        var status = new Dictionary<string, ConsolidationStatus>();
        
        // Get all memories for user
        var allMemories = await _baseMemory.RetrieveAsync(userId, "", 100); // Get all
        
        foreach (var memory in allMemories)
        {
            var concepts = ExtractConcepts(memory.Content);
            foreach (var concept in concepts)
            {
                var consolidationStatus = userGraph.GetConsolidationStatus(concept);
                status[concept] = consolidationStatus;
            }
        }
        
        return status;
    }

    public async Task DeleteMemoryAsync(string userId, string memoryId)
    {
        await _baseMemory.DeleteMemoryAsync(userId, memoryId);
    }

    public async Task ClearMemoriesAsync(string userId)
    {
        await _baseMemory.ClearMemoriesAsync(userId);
        _userGraphs.Remove(userId);
    }

    private HebbianMemoryGraph GetUserGraph(string userId)
    {
        if (!_userGraphs.ContainsKey(userId))
        {
            _userGraphs[userId] = new HebbianMemoryGraph();
        }
        return _userGraphs[userId];
    }

    private List<string> ExtractConcepts(string text)
    {
        // Simple concept extraction
        var words = text.Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '(', ')', '\n', '\r' }, 
            StringSplitOptions.RemoveEmptyEntries);
        
        return words
            .Where(w => w.Length > 3)
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .ToList();
    }
}
