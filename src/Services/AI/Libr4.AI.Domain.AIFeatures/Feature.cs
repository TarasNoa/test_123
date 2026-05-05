using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.AIFeatures;

public enum FeatureCategory { CodeAnalysis, Translation, Generation, Classification, Optimization }

public class Feature
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FeatureCategory Category { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsPremium { get; set; }
    public Dictionary<string, object> Config { get; set; } = new Dictionary<string, object>();
    public int UsageCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class UserFeatureAccess
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FeatureId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public bool CanUse() => IsEnabled && UsageCount < (UsageLimit ?? int.MaxValue) && 
                             (!ExpiresAt.HasValue || DateTimeOffset.UtcNow < ExpiresAt.Value);
}
