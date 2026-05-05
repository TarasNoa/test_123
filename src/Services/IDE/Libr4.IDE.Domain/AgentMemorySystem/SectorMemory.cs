namespace Libr4.IDE.Domain.AgentMemorySystem;

/// <summary>
/// A memory sector for storing fragments
/// </summary>
public class SectorMemory
{
    public string SectorId { get; set; } = string.Empty;
    public MemorySector Sector { get; set; }
    public List<MemoryFragment> Fragments { get; private set; } = new();
    public int Capacity { get; set; }
    public int CurrentUsage { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public TimeSpan RetentionPeriod { get; set; }
    public MemoryCompressionLevel CompressionLevel { get; set; }
    
    public SectorMemory()
    {
    }
    
    public SectorMemory(string sectorId, MemorySector sector, int? capacity = null, TimeSpan? retentionPeriod = null)
    {
        SectorId = sectorId;
        Sector = sector;
        Capacity = capacity ?? sector.DefaultCapacity();
        RetentionPeriod = retentionPeriod ?? sector.DefaultRetentionPeriod();
        CompressionLevel = MemoryCompressionLevel.Medium;
        LastAccessedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Add a fragment to the sector
    /// </summary>
    public void AddFragment(MemoryFragment fragment)
    {
        if (CurrentUsage >= Capacity)
        {
            EvictOldestFragment();
        }
        
        Fragments.Add(fragment);
        CurrentUsage++;
        LastAccessedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Evict the oldest fragment
    /// </summary>
    private void EvictOldestFragment()
    {
        var oldest = Fragments.OrderBy(f => f.CreatedAt).FirstOrDefault();
        if (oldest != null)
        {
            Fragments.Remove(oldest);
            CurrentUsage--;
        }
    }
    
    /// <summary>
    /// Get active (non-expired) fragments
    /// </summary>
    public List<MemoryFragment> GetActiveFragments()
    {
        return Fragments.Where(f => !f.IsExpired()).ToList();
    }
    
    /// <summary>
    /// Get fragments by query
    /// </summary>
    public List<MemoryFragment> GetFragments(string query)
    {
        var activeFragments = GetActiveFragments();
        return activeFragments
            .Where(f => f.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
    
    /// <summary>
    /// Remove a fragment
    /// </summary>
    public void RemoveFragment(string fragmentId)
    {
        var fragment = Fragments.FirstOrDefault(f => f.Id.ToString() == fragmentId);
        if (fragment != null)
        {
            Fragments.Remove(fragment);
            CurrentUsage--;
        }
    }
    
    /// <summary>
    /// Clear all fragments
    /// </summary>
    public void Clear()
    {
        Fragments.Clear();
        CurrentUsage = 0;
    }
    
    /// <summary>
    /// Get usage percentage
    /// </summary>
    public double UsagePercentage => Capacity > 0 ? (double)CurrentUsage / Capacity * 100.0 : 0.0;
    
    /// <summary>
    /// Check if sector is full
    /// </summary>
    public bool IsFull => CurrentUsage >= Capacity;
    
    /// <summary>
    /// Access the sector
    /// </summary>
    public void Access()
    {
        LastAccessedAt = DateTime.UtcNow;
    }
}
