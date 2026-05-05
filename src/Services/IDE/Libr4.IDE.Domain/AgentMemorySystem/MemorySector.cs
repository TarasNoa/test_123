namespace Libr4.IDE.Domain.AgentMemorySystem;

/// <summary>
/// Memory sector types for multi-sector memory system
/// </summary>
public enum MemorySector
{
    ShortTerm,      // Working memory, quickly forgotten
    LongTerm,       // Long-term memory
    Episodic,       // Episodic memory (events)
    Semantic,       // Semantic memory (facts, concepts)
    Procedural,     // Procedural memory (skills)
    Working         // Working memory for current task
}

/// <summary>
/// Extension methods for MemorySector
/// </summary>
public static class MemorySectorExtensions
{
    /// <summary>
    /// Get default retention period for a sector
    /// </summary>
    public static TimeSpan DefaultRetentionPeriod(this MemorySector sector) => sector switch
    {
        MemorySector.ShortTerm => TimeSpan.FromMinutes(30),
        MemorySector.LongTerm => TimeSpan.FromDays(365),
        MemorySector.Episodic => TimeSpan.FromDays(90),
        MemorySector.Semantic => TimeSpan.FromDays(365),
        MemorySector.Procedural => TimeSpan.FromDays(365),
        MemorySector.Working => TimeSpan.FromHours(4),
        _ => TimeSpan.FromDays(30)
    };
    
    /// <summary>
    /// Get default capacity for a sector
    /// </summary>
    public static int DefaultCapacity(this MemorySector sector) => sector switch
    {
        MemorySector.ShortTerm => 50,
        MemorySector.LongTerm => 1000,
        MemorySector.Episodic => 500,
        MemorySector.Semantic => 1000,
        MemorySector.Procedural => 200,
        MemorySector.Working => 100,
        _ => 100
    };
    
    /// <summary>
    /// Check if sector is volatile (short-lived)
    /// </summary>
    public static bool IsVolatile(this MemorySector sector) => sector switch
    {
        MemorySector.ShortTerm => true,
        MemorySector.Working => true,
        _ => false
    };
}
