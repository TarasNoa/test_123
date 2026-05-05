namespace Libr4.Tasks.Domain.AITaskAnalysis;

public class TaskAIAnalysisService
{
    public TaskAIAnalysisResult AnalyzeTask(string title, string description, string category)
    {
        // Rule-based analysis (fallback for ML)
        var complexity = EstimateComplexity(title, description);
        var recommendedSkills = ExtractSkills(title, description, category);
        var estimatedDuration = EstimateDuration(complexity);
        var suggestedBudget = SuggestBudget(complexity, estimatedDuration);
        var challenges = IdentifyChallenges(description, complexity);
        var successFactors = IdentifySuccessFactors(title, description);

        return new TaskAIAnalysisResult
        {
            Complexity = complexity,
            RecommendedSkills = recommendedSkills,
            EstimatedDuration = estimatedDuration,
            SuggestedBudget = suggestedBudget,
            Challenges = challenges,
            SuccessFactors = successFactors,
            AnalysisTimestamp = DateTimeOffset.UtcNow
        };
    }

    private int EstimateComplexity(string title, string description)
    {
        var score = 5; // Base complexity

        // Analyze title
        if (title.ToLower().Contains("complex") || title.ToLower().Contains("enterprise"))
            score += 2;
        if (title.ToLower().Contains("simple") || title.ToLower().Contains("basic"))
            score -= 1;

        // Analyze description length
        var descriptionLength = description.Length;
        if (descriptionLength > 2000)
            score += 2;
        else if (descriptionLength > 1000)
            score += 1;

        // Analyze keywords
        var keywords = new[] { "api", "integration", "database", "microservices", "ai", "ml", "blockchain", "security" };
        foreach (var keyword in keywords)
        {
            if (description.ToLower().Contains(keyword))
                score += 1;
        }

        return Math.Max(1, Math.Min(10, score));
    }

    private List<string> ExtractSkills(string title, string description, string category)
    {
        var skills = new List<string>();

        // Add category as a skill
        if (!string.IsNullOrEmpty(category))
        {
            skills.Add(category.ToLower());
        }

        // Common tech skills
        var techSkills = new Dictionary<string, string[]>
        {
            ["web"] = new[] { "html", "css", "javascript", "react", "vue", "angular", "node.js", "typescript" },
            ["mobile"] = new[] { "ios", "android", "swift", "kotlin", "flutter", "react native" },
            ["backend"] = new[] { "python", "java", "c#", ".net", "node.js", "go", "rust", "sql" },
            ["ai"] = new[] { "python", "tensorflow", "pytorch", "ml", "nlp", "computer vision" },
            ["design"] = new[] { "figma", "sketch", "photoshop", "illustrator", "ui", "ux" }
        };

        var combinedText = $"{title} {description}".ToLower();
        foreach (var kvp in techSkills)
        {
            if (combinedText.Contains(kvp.Key))
            {
                foreach (var skill in kvp.Value)
                {
                    if (combinedText.Contains(skill) && !skills.Contains(skill))
                        skills.Add(skill);
                }
            }
        }

        return skills;
    }

    private string EstimateDuration(int complexity)
    {
        return complexity switch
        {
            <= 3 => "1-3 days",
            <= 5 => "3-7 days",
            <= 7 => "1-2 weeks",
            <= 9 => "2-4 weeks",
            _ => "4-8 weeks"
        };
    }

    private string SuggestBudget(int complexity, string duration)
    {
        var baseBudget = complexity * 100;
        
        if (duration.Contains("week"))
            baseBudget *= 5;
        else if (duration.Contains("month"))
            baseBudget *= 20;

        return $"${baseBudget}-{baseBudget * 2}";
    }

    private List<string> IdentifyChallenges(string description, int complexity)
    {
        var challenges = new List<string>();

        if (complexity >= 7)
            challenges.Add("High complexity - may require team coordination");

        if (description.ToLower().Contains("legacy"))
            challenges.Add("Legacy code integration required");

        if (description.ToLower().Contains("deadline") || description.ToLower().Contains("urgent"))
            challenges.Add("Tight timeline - may impact quality");

        if (description.ToLower().Contains("security") || description.ToLower().Contains("encryption"))
            challenges.Add("Security requirements add complexity");

        if (description.ToLower().Contains("scalable") || description.ToLower().Contains("high traffic"))
            challenges.Add("Scalability considerations needed");

        return challenges;
    }

    private List<string> IdentifySuccessFactors(string title, string description)
    {
        var factors = new List<string>();

        if (!string.IsNullOrEmpty(title))
            factors.Add("Clear project requirements");

        if (description.ToLower().Contains("documentation") || description.ToLower().Contains("specs"))
            factors.Add("Good documentation available");

        if (description.ToLower().Contains("api"))
            factors.Add("Well-defined API contracts");

        if (description.ToLower().Contains("test") || description.ToLower().Contains("testing"))
            factors.Add("Testing requirements specified");

        factors.Add("Clear communication channels");
        factors.Add("Realistic timeline expectations");

        return factors;
    }
}

public class TaskAIAnalysisResult
{
    public int Complexity { get; set; }
    public List<string> RecommendedSkills { get; set; } = new();
    public string EstimatedDuration { get; set; } = string.Empty;
    public string SuggestedBudget { get; set; } = string.Empty;
    public List<string> Challenges { get; set; } = new();
    public List<string> SuccessFactors { get; set; } = new();
    public DateTimeOffset AnalysisTimestamp { get; set; }
}
