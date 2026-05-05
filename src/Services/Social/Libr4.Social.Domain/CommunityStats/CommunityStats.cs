using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Social.Domain.CommunityStats.Events;

namespace Libr4.Social.Domain.CommunityStats;

public class CommunityStats : AggregateRoot<Guid>
{
    public string CommunityName { get; private set; } = string.Empty;
    public int TotalMembers { get; private set; }
    public int ActiveMembers { get; private set; }
    public int TotalPosts { get; private set; }
    public int TotalInteractions { get; private set; }
    public float EngagementRate { get; private set; }
    public float GrowthRate { get; private set; }
    public DateTimeOffset LastCalculatedAt { get; private set; }

    private CommunityStats() { }

    public void UpdateStats(int totalMembers, int activeMembers, int totalPosts, int totalInteractions, DateTimeOffset now)
    {
        TotalMembers = totalMembers;
        ActiveMembers = activeMembers;
        TotalPosts = totalPosts;
        TotalInteractions = totalInteractions;
        
        EngagementRate = totalMembers > 0 ? (float)totalInteractions / (float)totalMembers * 100f : 0f;
        GrowthRate = totalMembers > 0 ? (float)activeMembers / (float)totalMembers * 100f : 0f;
        
        LastCalculatedAt = now;
        RaiseDomainEvent(new CommunityStatsUpdatedEvent(Id, CommunityName, totalMembers, activeMembers, now));
    }
}

public class CommunityMemberStats
{
    public Guid Id { get; set; }
    public Guid CommunityId { get; set; } = Guid.Empty;
    public Guid UserId { get; set; } = Guid.Empty;
    public int PostsCount { get; set; }
    public int InteractionsCount { get; set; }
    public int ConnectionsCount { get; set; }
    public float ActivityScore { get; private set; }
    public DateTime LastActiveAt { get; set; }

    public void UpdateActivityScore()
    {
        ActivityScore = (PostsCount * 2f + InteractionsCount * 1f + ConnectionsCount * 0.5f) / 3f;
    }
}
