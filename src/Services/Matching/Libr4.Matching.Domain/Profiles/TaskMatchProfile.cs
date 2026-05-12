namespace Libr4.Matching.Domain.Profiles;

public class TaskMatchProfile
{
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> RequiredSkills { get; set; } = new();
    public int BudgetMin { get; set; }
    public int BudgetMax { get; set; }
    public int DurationDays { get; set; }
    public DateTimeOffset PostedAt { get; set; }
    public float[]? Embedding { get; set; }
}
