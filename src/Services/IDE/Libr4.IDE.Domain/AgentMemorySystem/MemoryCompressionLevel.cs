namespace Libr4.IDE.Domain.AgentMemorySystem;

/// <summary>
/// Represents the compression level for memory fragments
/// </summary>
public enum MemoryCompressionLevel
{
    /// <summary>
    /// No compression
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Basic compression
    /// </summary>
    Low = 1,
    
    /// <summary>
    /// Advanced compression
    /// </summary>
    Medium = 2,
    
    /// <summary>
    /// Aggressive compression
    /// </summary>
    High = 3
}
