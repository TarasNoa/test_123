namespace Libr4.Matching.Domain.Profiles;

public class FreelancerMatchProfile
{
    public Guid FreelancerId { get; set; }
    public List<string> Skills { get; set; } = new();
    public List<string> Interests { get; set; } = new();
    public double AverageRating { get; set; }
    public int CompletedTasks { get; set; }
    public int HourlyRateMin { get; set; }
    public int HourlyRateMax { get; set; }
    public float[]? Embedding { get; set; }
    public DateTimeOffset IndexedAt { get; set; }
}
