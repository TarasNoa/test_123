using System.Text.Json;
using System.Text.RegularExpressions;
using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Application.CVAnalysis;

public interface ICVAnalysisService
{
    Task<CVAnalysisResult> AnalyzeAsync(CVAnalysisRequest request, CancellationToken ct = default);
}

public sealed class CVAnalysisService : ICVAnalysisService
{
    private readonly IAIService _aiService;
    private readonly ILogger<CVAnalysisService> _logger;

    public CVAnalysisService(IAIService aiService, ILogger<CVAnalysisService> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<CVAnalysisResult> AnalyzeAsync(CVAnalysisRequest request, CancellationToken ct = default)
    {
        var combinedText = BuildCombinedText(request);
        _logger.LogInformation("Analyzing CV for user {UserId}, text length: {Length}", request.UserId, combinedText.Length);

        var systemPrompt = """
You are an expert technical recruiter and skill assessor.
Analyze the provided CV and LinkedIn profile data.
Return ONLY a JSON object with this exact structure:
{
  "skills": [
    {"name": "Python", "score": 95, "level": "Expert", "source": "cv|linkedin", "experienceYears": 8, "contexts": ["backend", "ml"]}
  ],
  "overallLevel": "Senior",
  "overallScore": 88,
  "primaryExpertise": "Machine Learning Engineering",
  "secondaryExpertise": ["Data Engineering", "MLOps"],
  "recommendations": ["Consider cloud certifications", "Strengthen system design skills"]
}
Score rules: 0-25 Beginner, 26-50 Intermediate, 51-75 Advanced, 76-100 Expert.
Be precise and realistic based on the experience described.
""";

        string aiResponse;
        try
        {
            aiResponse = await _aiService.AnalyzeTextAsync(combinedText, "cv-analysis", systemPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI service failed, falling back to heuristic parsing");
            return HeuristicParse(request, combinedText);
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(aiResponse);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "AI returned invalid JSON, falling back to heuristic parsing");
            return HeuristicParse(request, combinedText);
        }

        var root = doc.RootElement;
        var skills = new List<ExtractedSkill>();

        if (root.TryGetProperty("skills", out var skillsArray))
        {
            foreach (var s in skillsArray.EnumerateArray())
            {
                skills.Add(new ExtractedSkill(
                    Name: s.GetProperty("name").GetString() ?? "Unknown",
                    Score: s.TryGetProperty("score", out var scoreElem) ? scoreElem.GetSingle() : 50,
                    Level: s.TryGetProperty("level", out var levelElem) ? levelElem.GetString() ?? "Intermediate" : "Intermediate",
                    Source: s.TryGetProperty("source", out var srcElem) ? srcElem.GetString() ?? "combined" : "combined",
                    ExperienceYears: s.TryGetProperty("experienceYears", out var expElem) ? expElem.GetInt32() : 0,
                    Contexts: s.TryGetProperty("contexts", out var ctxElem)
                        ? ctxElem.EnumerateArray().Select(c => c.GetString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList()
                        : new List<string>()));
            }
        }

        return new CVAnalysisResult(
            UserId: request.UserId,
            Skills: skills.OrderByDescending(s => s.Score).ToList(),
            OverallLevel: root.TryGetProperty("overallLevel", out var ol) ? ol.GetString() ?? "Mid" : "Mid",
            OverallScore: root.TryGetProperty("overallScore", out var os) ? os.GetSingle() : 50,
            PrimaryExpertise: root.TryGetProperty("primaryExpertise", out var pe) ? pe.GetString() ?? "General" : "General",
            SecondaryExpertise: root.TryGetProperty("secondaryExpertise", out var se)
                ? se.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList()
                : new List<string>(),
            Recommendations: root.TryGetProperty("recommendations", out var rec)
                ? rec.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList()
                : new List<string>(),
            AnalyzedAt: DateTimeOffset.UtcNow);
    }

    private static string BuildCombinedText(CVAnalysisRequest request)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.CvText))
            parts.Add($"CV TEXT:\n{request.CvText}");

        if (request.LinkedInData is not null)
        {
            parts.Add($"LINKEDIN:\nHeadline: {request.LinkedInData.Headline}\nSummary: {request.LinkedInData.Summary}");
            foreach (var exp in request.LinkedInData.Experience)
            {
                parts.Add($"- {exp.Title} at {exp.Company} ({exp.DurationYears} years). Skills: {string.Join(", ", exp.Skills)}");
            }
            foreach (var edu in request.LinkedInData.Education)
            {
                parts.Add($"- Education: {edu.Degree}, {edu.School} ({edu.Year})");
            }
            parts.Add($"LinkedIn Skills: {string.Join(", ", request.LinkedInData.Skills)}");
        }

        return string.Join("\n\n", parts);
    }

    private static CVAnalysisResult HeuristicParse(CVAnalysisRequest request, string text)
    {
        var skills = new List<ExtractedSkill>();
        var knownSkills = new[] { "python", "c#", "csharp", "javascript", "typescript", "java", "go", "rust", "cpp", "c++",
            "react", "angular", "vue", "node", "django", "flask", "fastapi", "spring",
            "sql", "postgresql", "mysql", "mongodb", "redis", "elasticsearch",
            "docker", "kubernetes", "aws", "azure", "gcp", "terraform", "ci/cd",
            "machine learning", "deep learning", "nlp", "computer vision", "pytorch", "tensorflow", "keras", "scikit-learn",
            "data science", "pandas", "numpy", "spark", "hadoop", "kafka",
            "react native", "flutter", "swift", "kotlin", "android", "ios",
            "html", "css", "sass", "tailwind", "bootstrap" };

        var textLower = text.ToLowerInvariant();
        foreach (var skill in knownSkills)
        {
            if (textLower.Contains(skill))
            {
                var years = ExtractYears(textLower, skill);
                var score = Math.Min(95, 40 + years * 8);
                var level = score switch { < 25 => "Beginner", < 50 => "Intermediate", < 75 => "Advanced", _ => "Expert" };
                skills.Add(new ExtractedSkill(skill, score, level, "heuristic", years, new List<string>()));
            }
        }

        var overall = skills.Count > 0 ? skills.Average(s => s.Score) : 50;
        return new CVAnalysisResult(
            request.UserId,
            skills.OrderByDescending(s => s.Score).ToList(),
            overall switch { < 25 => "Junior", < 50 => "Mid", < 75 => "Senior", _ => "Principal" },
            (float)overall,
            skills.FirstOrDefault().Name ?? "General",
            skills.Skip(1).Take(3).Select(s => s.Name).ToList(),
            new List<string>(),
            DateTimeOffset.UtcNow);
    }

    private static int ExtractYears(string text, string skill)
    {
        var pattern = $"\\b\\d+\\+?\\s*years?\\s+(?:of\\s+)?experience.*?{Regex.Escape(skill)}";
        var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(new string(match.Value.TakeWhile(char.IsDigit).ToArray()), out var y))
            return y;

        var genericPattern = $"{Regex.Escape(skill)}.*?\\b\\d+\\+?\\s*years?";
        var genericMatch = System.Text.RegularExpressions.Regex.Match(text, genericPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (genericMatch.Success && int.TryParse(new string(genericMatch.Value.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray()), out var y2))
            return y2;

        return 2;
    }
}
