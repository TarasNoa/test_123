namespace Libr4.Matching.Domain.Profiles;

public class FreelancerMatchProfile
{
    public Guid FreelancerId { get; set; }

    public List<string> Skills { get; set; } = new();
    public Dictionary<string, float> SkillScores { get; set; } = new();
    public Dictionary<string, string> SkillLevels { get; set; } = new();
    public Dictionary<string, int> SkillExperienceYears { get; set; } = new();

    public string OverallLevel { get; set; } = "Unknown";
    public float OverallScore { get; set; } = 0;
    public string PrimaryExpertise { get; set; } = "";
    public List<string> SecondaryExpertise { get; set; } = new();

    public List<string> Interests { get; set; } = new();
    public double AverageRating { get; set; }
    public int CompletedTasks { get; set; }
    public int HourlyRateMin { get; set; }
    public int HourlyRateMax { get; set; }

    public float[]? Embedding { get; set; }
    public DateTimeOffset IndexedAt { get; set; }

    public string BuildEmbeddingText()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(PrimaryExpertise))
        {
            parts.Add(PrimaryExpertise);
            parts.Add(PrimaryExpertise);
        }
        parts.AddRange(SecondaryExpertise);

        if (SkillScores.Any())
        {
            foreach (var (skill, score) in SkillScores.OrderByDescending(x => x.Value))
            {
                var repetitions = score >= 80 ? 3 : score >= 50 ? 2 : 1;
                for (var i = 0; i < repetitions; i++)
                    parts.Add(skill);

                if (SkillLevels.TryGetValue(skill, out var level) && level is "Advanced" or "Expert")
                    parts.Add($"{skill} {level}");
            }
        }
        else
        {
            parts.AddRange(Skills);
        }

        parts.AddRange(Interests);

        parts.Add(OverallLevel switch
        {
            "Junior" => "junior developer entry level",
            "Mid"    => "mid level developer intermediate",
            "Senior" => "senior developer experienced",
            "Expert" => "expert developer principal architect",
            _        => ""
        });

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
