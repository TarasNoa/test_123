namespace Libr4.AI.Infrastructure.Profile;

/// <summary>
/// User Profile Extractor - extracts structured data from user messages
/// Based on NGT Memory pattern for structured user profiles
/// </summary>
public interface IUserProfileExtractor
{
    /// <summary>
    /// Extract profile information from message
    /// </summary>
    Task<UserProfile> ExtractFromMessageAsync(string userId, string message);
    
    /// <summary>
    /// Get complete user profile
    /// </summary>
    Task<UserProfile> GetProfileAsync(string userId);
    
    /// <summary>
    /// Update profile slot
    /// </summary>
    Task UpdateSlotAsync(string userId, string slotName, object value);
    
    /// <summary>
    /// Get formatted profile for LLM context
    /// </summary>
    Task<string> GetFormattedProfileAsync(string userId);
    
    /// <summary>
    /// Detect conflicts (e.g., age decreasing)
    /// </summary>
    Task<List<ProfileConflict>> DetectConflictsAsync(string userId, UserProfile newProfile);
}

public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? City { get; set; }
    public string? Diet { get; set; }
    public List<string> Allergies { get; set; } = new();
    public string? Occupation { get; set; }
    public List<string> Interests { get; set; } = new();
    public Dictionary<string, object> CustomSlots { get; set; } = new();
    public DateTimeOffset LastUpdated { get; set; }
}

public class ProfileConflict
{
    public string SlotName { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ConflictResolution Resolution { get; set; }
}

public enum ConflictResolution
{
    Block, // Don't allow change
    AllowWithConfirmation, // Allow but require explicit confirmation
    Allow // Allow automatically
}
