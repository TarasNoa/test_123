using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Profile;

/// <summary>
/// Implementation of user profile extractor with regex-based slot extraction
/// </summary>
public class UserProfileExtractor : IUserProfileExtractor
{
    private readonly ILogger<UserProfileExtractor> _logger;
    private readonly Dictionary<string, UserProfile> _profiles = new();
    private readonly Dictionary<string, List<string>> _fragmentBuffers = new();

    public UserProfileExtractor(ILogger<UserProfileExtractor> logger)
    {
        _logger = logger;
    }

    public async Task<UserProfile> ExtractFromMessageAsync(string userId, string message)
    {
        var profile = await GetProfileAsync(userId);
        
        // Add message to fragment buffer for cross-message extraction
        AddToFragmentBuffer(userId, message);
        
        // Extract slots from message
        ExtractAge(profile, message);
        ExtractCity(profile, message);
        ExtractDiet(profile, message);
        ExtractAllergies(profile, message);
        ExtractName(profile, message);
        ExtractOccupation(profile, message);
        
        // Check for fragment assembly
        TryAssembleFragments(userId, profile);
        
        profile.LastUpdated = DateTimeOffset.UtcNow;
        _profiles[userId] = profile;
        
        _logger.LogDebug("Extracted profile for user {UserId}: Age={Age}, City={City}", 
            userId, profile.Age, profile.City);
        
        return profile;
    }

    public async Task<UserProfile> GetProfileAsync(string userId)
    {
        if (_profiles.TryGetValue(userId, out var profile))
            return profile;
        
        return new UserProfile { UserId = userId };
    }

    public async Task UpdateSlotAsync(string userId, string slotName, object value)
    {
        var profile = await GetProfileAsync(userId);
        
        switch (slotName.ToLowerInvariant())
        {
            case "name":
                profile.Name = value.ToString();
                break;
            case "age":
                profile.Age = Convert.ToInt32(value);
                break;
            case "city":
                profile.City = value.ToString();
                break;
            case "diet":
                profile.Diet = value.ToString();
                break;
            case "occupation":
                profile.Occupation = value.ToString();
                break;
            default:
                profile.CustomSlots[slotName] = value;
                break;
        }
        
        profile.LastUpdated = DateTimeOffset.UtcNow;
        _profiles[userId] = profile;
    }

    public async Task<string> GetFormattedProfileAsync(string userId)
    {
        var profile = await GetProfileAsync(userId);
        
        var formatted = new System.Text.StringBuilder();
        formatted.AppendLine("[USER PROFILE - structured facts, highest priority]");
        
        if (!string.IsNullOrEmpty(profile.Name))
            formatted.AppendLine($"- name: {profile.Name}");
        
        if (profile.Age.HasValue)
            formatted.AppendLine($"- age: {profile.Age}");
        
        if (!string.IsNullOrEmpty(profile.City))
            formatted.AppendLine($"- city: {profile.City}");
        
        if (!string.IsNullOrEmpty(profile.Diet))
            formatted.AppendLine($"- diet: {profile.Diet}");
        
        if (profile.Allergies.Any())
            formatted.AppendLine($"- allergies: {string.Join(", ", profile.Allergies)}");
        
        if (!string.IsNullOrEmpty(profile.Occupation))
            formatted.AppendLine($"- occupation: {profile.Occupation}");
        
        if (profile.Interests.Any())
            formatted.AppendLine($"- interests: {string.Join(", ", profile.Interests)}");
        
        foreach (var slot in profile.CustomSlots)
        {
            formatted.AppendLine($"- {slot.Key}: {slot.Value}");
        }
        
        formatted.AppendLine("[END USER PROFILE]");
        
        return formatted.ToString();
    }

    public async Task<List<ProfileConflict>> DetectConflictsAsync(string userId, UserProfile newProfile)
    {
        var conflicts = new List<ProfileConflict>();
        var existingProfile = await GetProfileAsync(userId);
        
        // Age should only increase
        if (existingProfile.Age.HasValue && newProfile.Age.HasValue)
        {
            if (newProfile.Age.Value < existingProfile.Age.Value)
            {
                conflicts.Add(new ProfileConflict
                {
                    SlotName = "age",
                    OldValue = existingProfile.Age.Value,
                    NewValue = newProfile.Age.Value,
                    Reason = "Age cannot decrease without explicit correction",
                    Resolution = ConflictResolution.Block
                });
            }
        }
        
        // City change is allowed but might need confirmation
        if (!string.IsNullOrEmpty(existingProfile.City) && 
            !string.IsNullOrEmpty(newProfile.City) &&
            existingProfile.City != newProfile.City)
        {
            conflicts.Add(new ProfileConflict
            {
                SlotName = "city",
                OldValue = existingProfile.City,
                NewValue = newProfile.City,
                Reason = "City changed - possible relocation",
                Resolution = ConflictResolution.Allow
            });
        }
        
        return conflicts;
    }

    private void ExtractAge(UserProfile profile, string message)
    {
        // Patterns: "I'm 30", "age: 30", "30 years old"
        var patterns = new[]
        {
            @"(?:I'm|I am)\s+(\d+)",
            @"age[:\s]+(\d+)",
            @"(\d+)\s+years?\s+old"
        };
        
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var age))
            {
                if (age > 0 && age < 150)
                {
                    profile.Age = age;
                    return;
                }
            }
        }
    }

    private void ExtractCity(UserProfile profile, string message)
    {
        // Simple extraction - in production would use NER
        var patterns = new[]
            {
                @"live\s+(?:in|at)\s+([A-Z][a-zA-Z\s]+)",
                @"from\s+([A-Z][a-zA-Z\s]+)",
                @"city[:\s]+([A-Z][a-zA-Z\s]+)"
            };
        
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                profile.City = match.Groups[1].Value.Trim();
                return;
            }
        }
    }

    private void ExtractDiet(UserProfile profile, string message)
    {
        var diets = new[] { "vegetarian", "vegan", "pescatarian", "keto", "paleo", "halal", "kosher" };
        
        foreach (var diet in diets)
        {
            if (message.Contains(diet, StringComparison.OrdinalIgnoreCase))
            {
                profile.Diet = diet;
                return;
            }
        }
    }

    private void ExtractAllergies(UserProfile profile, string message)
    {
        var commonAllergies = new[] { "peanut", "nuts", "dairy", "gluten", "shellfish", "soy", "eggs" };
        
        foreach (var allergy in commonAllergies)
        {
            if (message.Contains(allergy, StringComparison.OrdinalIgnoreCase) ||
                message.Contains($"allergic to {allergy}", StringComparison.OrdinalIgnoreCase))
            {
                if (!profile.Allergies.Contains(allergy))
                {
                    profile.Allergies.Add(allergy);
                }
            }
        }
    }

    private void ExtractName(UserProfile profile, string message)
    {
        // Pattern: "I'm [Name]", "My name is [Name]"
        var patterns = new[]
        {
            @"(?:I'm|I am|my name is)\s+([A-Z][a-zA-Z]+)",
            @"call\s+me\s+([A-Z][a-zA-Z]+)"
        };
        
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                profile.Name = match.Groups[1].Value;
                return;
            }
        }
    }

    private void ExtractOccupation(UserProfile profile, string message)
    {
        var occupations = new[] { "developer", "engineer", "designer", "manager", "student", "teacher", "doctor" };
        
        foreach (var occupation in occupations)
        {
            if (message.Contains($"I'm a {occupation}", StringComparison.OrdinalIgnoreCase) ||
                message.Contains($"I am a {occupation}", StringComparison.OrdinalIgnoreCase))
            {
                profile.Occupation = occupation;
                return;
            }
        }
    }

    private void AddToFragmentBuffer(string userId, string message)
    {
        if (!_fragmentBuffers.ContainsKey(userId))
        {
            _fragmentBuffers[userId] = new List<string>();
        }
        
        _fragmentBuffers[userId].Add(message);
        
        // Keep buffer limited to last 10 messages
        if (_fragmentBuffers[userId].Count > 10)
        {
            _fragmentBuffers[userId].RemoveAt(0);
        }
    }

    private void TryAssembleFragments(string userId, UserProfile profile)
    {
        if (!_fragmentBuffers.ContainsKey(userId))
            return;
        
        var buffer = _fragmentBuffers[userId];
        if (buffer.Count < 2)
            return;
        
        // Try to assemble fragments like "me", "30", "years" -> "me 30 years"
        var combined = string.Join(" ", buffer.TakeLast(3));
        
        // Re-extract with combined context
        ExtractAge(profile, combined);
        ExtractCity(profile, combined);
    }
}
