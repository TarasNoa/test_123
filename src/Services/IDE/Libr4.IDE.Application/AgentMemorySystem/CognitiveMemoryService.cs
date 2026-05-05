namespace Libr4.IDE.Application.AgentMemorySystem;

using Libr4.IDE.Domain.AgentMemorySystem;

/// <summary>
/// Service for cognitive memory system management
/// </summary>
public interface ICognitiveMemoryService
{
    /// <summary>
    /// Create a new cognitive memory system for an agent
    /// </summary>
    CognitiveMemorySystem CreateMemorySystem(string agentId);
    
    /// <summary>
    /// Store a memory fragment in a sector
    /// </summary>
    void StoreFragment(string systemId, MemorySector sector, MemoryFragment fragment);
    
    /// <summary>
    /// Retrieve fragments from a sector
    /// </summary>
    List<MemoryFragment> RetrieveFragments(string systemId, MemorySector sector, string query);
    
    /// <summary>
    /// Retrieve all fragments from a sector
    /// </summary>
    List<MemoryFragment> RetrieveAllFromSector(string systemId, MemorySector sector);
    
    /// <summary>
    /// Search across all sectors
    /// </summary>
    List<MemoryFragment> SearchAll(string systemId, string query);
    
    /// <summary>
    /// Add cross-reference between fragments
    /// </summary>
    void AddCrossReference(string systemId, string fromFragmentId, string toFragmentId, string relationshipType, float weight = 1.0f);
    
    /// <summary>
    /// Get related fragments
    /// </summary>
    List<MemoryFragment> GetRelatedFragments(string systemId, string fragmentId);
    
    /// <summary>
    /// Compress a sector
    /// </summary>
    void CompressSector(string systemId, MemorySector sector, MemoryCompressionLevel level);
    
    /// <summary>
    /// Get memory system statistics
    /// </summary>
    MemorySystemStatistics GetStatistics(string systemId);
    
    /// <summary>
    /// Get memory system by ID
    /// </summary>
    CognitiveMemorySystem? GetMemorySystem(string systemId);
    
    /// <summary>
    /// Delete a memory system
    /// </summary>
    void DeleteMemorySystem(string systemId);
}

public class CognitiveMemoryService : ICognitiveMemoryService
{
    private readonly Dictionary<string, CognitiveMemorySystem> _memorySystems = new();
    
    public CognitiveMemorySystem CreateMemorySystem(string agentId)
    {
        var systemId = Guid.NewGuid().ToString();
        var system = new CognitiveMemorySystem(systemId, agentId);
        _memorySystems[systemId] = system;
        return system;
    }
    
    public void StoreFragment(string systemId, MemorySector sector, MemoryFragment fragment)
    {
        if (!_memorySystems.TryGetValue(systemId, out var system))
        {
            throw new ArgumentException($"Memory system {systemId} not found");
        }
        
        system.StoreInSector(sector, fragment);
    }
    
    public List<MemoryFragment> RetrieveFragments(string systemId, MemorySector sector, string query)
    {
        if (!_memorySystems.TryGetValue(systemId, out var system))
        {
            throw new ArgumentException($"Memory system {systemId} not found");
        }
        
        return system.RetrieveFromSector(sector, query);
    }
    
    public List<MemoryFragment> RetrieveAllFromSector(string systemId, MemorySector sector)
    {
        if (!_memorySystems.TryGetValue(systemId, out var system))
        {
            throw new ArgumentException($"Memory system {systemId} not found");
        }
        
        return system.RetrieveAllFromSector(sector);
    }
    
    public List<MemoryFragment> SearchAll(string systemId, string query)
    {
        if (!_memorySystems.TryGetValue(systemId, out var system))
        {
            throw new ArgumentException($"Memory system {systemId} not found");
        }
        
        return system.SearchAll(query);
    }
    
    public void AddCrossReference(string systemId, string fromFragmentId, string toFragmentId, string relationshipType, float weight = 1.0f)
    {
        if (!_memorySystems.TryGetValue(systemId, out var system))
        {
            throw new ArgumentException($"Memory system {systemId} not found");
        }
        
        system.AddCrossReference(fromFragmentId, toFragmentId, relationshipType, weight);
    }
    
    public List<MemoryFragment> GetRelatedFragments(string systemId, string fragmentId)
    {
        if (!_memorySystems.TryGetValue(systemId, out var system))
        {
            throw new ArgumentException($"Memory system {systemId} not found");
        }
        
        return system.GetRelatedFragments(fragmentId);
    }
    
    public void CompressSector(string systemId, MemorySector sector, MemoryCompressionLevel level)
    {
        if (!_memorySystems.TryGetValue(systemId, out var system))
        {
            throw new ArgumentException($"Memory system {systemId} not found");
        }
        
        system.CompressSector(sector, level);
    }
    
    public MemorySystemStatistics GetStatistics(string systemId)
    {
        if (!_memorySystems.TryGetValue(systemId, out var system))
        {
            throw new ArgumentException($"Memory system {systemId} not found");
        }
        
        return system.GetStatistics();
    }
    
    public CognitiveMemorySystem? GetMemorySystem(string systemId)
    {
        return _memorySystems.TryGetValue(systemId, out var system) ? system : null;
    }
    
    public void DeleteMemorySystem(string systemId)
    {
        _memorySystems.Remove(systemId);
    }
}
