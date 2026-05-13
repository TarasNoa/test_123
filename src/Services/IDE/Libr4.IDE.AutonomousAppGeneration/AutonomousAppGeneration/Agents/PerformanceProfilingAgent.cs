using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Performance profiling agent with profiling, bundle analysis, and load testing
/// Inspired by claude-skills performance-profiler skill
/// </summary>
public class PerformanceProfilingAgent : AgentSkillBase
{
    private readonly IAIService _aiService;
    private readonly ILogger _logger;

    public PerformanceProfilingAgent(
        string skillPath,
        IAIService aiService,
        ILogger logger) : base(skillPath)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public override async Task<AgentResult> ExecuteAsync(AgentContext context)
    {
        _logger.LogInformation("Executing PerformanceProfilingAgent for application: {ApplicationName}", context.ApplicationName);

        var skillInstructions = GetSkillInstructions();
        var prompt = BuildPrompt(context, skillInstructions);

        var response = await _aiService.GenerateCompletionAsync(prompt, skillInstructions);

        var profile = ParsePerformanceProfile(response);

        _logger.LogInformation("Performance profile generated with {RecommendationCount} recommendations", profile.Recommendations.Count);

        return new AgentResult
        {
            IsSuccess = true,
            PerformanceProfile = profile,
            Content = response
        };
    }

    private string BuildPrompt(AgentContext context, string skillInstructions)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("Generate a performance profiling and optimization plan for the following application:");
        sb.AppendLine();
        sb.AppendLine($"Application Name: {context.ApplicationName}");
        sb.AppendLine($"Tech Stack: {context.TechStack}");
        sb.AppendLine($"Description: {context.Description}");
        sb.AppendLine();
        
        if (context.GeneratedFiles != null && context.GeneratedFiles.Any())
        {
            sb.AppendLine("Key files for analysis:");
            foreach (var file in context.GeneratedFiles.Where(f => 
                f.RelativePath.Contains("Controller") || 
                f.RelativePath.Contains("Service") ||
                f.RelativePath.Contains("Repository")))
            {
                sb.AppendLine($"- {file.RelativePath}");
                sb.AppendLine(file.Content.Substring(0, Math.Min(300, file.Content.Length)));
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("Please provide:");
        sb.AppendLine("1. Performance bottlenecks identification");
        sb.AppendLine("2. Caching strategies");
        sb.AppendLine("3. Database query optimization");
        sb.AppendLine("4. Bundle analysis (if applicable)");
        sb.AppendLine("5. Load testing plan");
        sb.AppendLine("6. Monitoring recommendations");
        sb.AppendLine("7. Performance metrics to track");

        return sb.ToString();
    }

    private PerformanceProfile ParsePerformanceProfile(string content)
    {
        var profile = new PerformanceProfile
        {
            Bottlenecks = new List<PerformanceBottleneck>(),
            Recommendations = new List<PerformanceRecommendation>(),
            MonitoringMetrics = new List<string>()
        };

        // Parse the AI response into structured performance profile
        var lines = content.Split('\n');
        var currentSection = string.Empty;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("##") || trimmed.StartsWith("#"))
            {
                currentSection = trimmed.Trim('#').Trim().ToLower();
            }
            else if (currentSection.Contains("bottleneck") || currentSection.Contains("issue"))
            {
                if (!string.IsNullOrEmpty(trimmed) && (trimmed.StartsWith("-") || trimmed.StartsWith("*")))
                {
                    var bottleneck = new PerformanceBottleneck
                    {
                        Description = trimmed.TrimStart('-', '*').Trim()
                    };
                    profile.Bottlenecks.Add(bottleneck);
                }
            }
            else if (currentSection.Contains("recommendation") || currentSection.Contains("optimization"))
            {
                if (!string.IsNullOrEmpty(trimmed) && (trimmed.StartsWith("-") || trimmed.StartsWith("*")))
                {
                    var recommendation = new PerformanceRecommendation
                    {
                        Description = trimmed.TrimStart('-', '*').Trim()
                    };
                    profile.Recommendations.Add(recommendation);
                }
            }
            else if (currentSection.Contains("metric") || currentSection.Contains("monitoring"))
            {
                if (!string.IsNullOrEmpty(trimmed) && (trimmed.StartsWith("-") || trimmed.StartsWith("*")))
                {
                    profile.MonitoringMetrics.Add(trimmed.TrimStart('-', '*').Trim());
                }
            }
        }

        return profile;
    }
}

/// <summary>
/// Performance profile structure
/// </summary>
public class PerformanceProfile
{
    public List<PerformanceBottleneck> Bottlenecks { get; set; } = new();
    public List<PerformanceRecommendation> Recommendations { get; set; } = new();
    public List<string> MonitoringMetrics { get; set; } = new();
    public string? LoadTestPlan { get; set; }
    public string? BundleAnalysis { get; set; }
    public Dictionary<string, string> CachingStrategies { get; set; } = new();
}

/// <summary>
/// Performance bottleneck
/// </summary>
public class PerformanceBottleneck
{
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int Severity { get; set; } // 1-10
    public string? Impact { get; set; }
}

/// <summary>
/// Performance recommendation
/// </summary>
public class PerformanceRecommendation
{
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string? Implementation { get; set; }
    public string? ExpectedImprovement { get; set; }
}
