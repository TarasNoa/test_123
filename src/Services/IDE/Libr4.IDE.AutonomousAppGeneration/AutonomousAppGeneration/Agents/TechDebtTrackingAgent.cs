using Libr4.AI.Infrastructure.AI;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Tech debt tracking agent with codebase debt scanner, prioritizer, and trend dashboard
/// Inspired by claude-skills tech-debt-tracker skill
/// </summary>
public class TechDebtTrackingAgent : AgentSkillBase
{
    private readonly IAIService _aiService;
    private readonly ILogger _logger;

    public TechDebtTrackingAgent(
        string skillPath,
        IAIService aiService,
        ILogger logger) : base(skillPath)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public override async Task<AgentResult> ExecuteAsync(AgentContext context)
    {
        _logger.LogInformation("Executing TechDebtTrackingAgent for application: {ApplicationName}", context.ApplicationName);

        var skillInstructions = GetSkillInstructions();
        var prompt = BuildPrompt(context, skillInstructions);

        var response = await _aiService.GenerateCompletionAsync(prompt, skillInstructions);

        var techDebt = ParseTechDebt(response);

        _logger.LogInformation("Tech debt analysis completed with {DebtCount} items, {HighPriorityCount} high priority", 
            techDebt.DebtItems.Count, 
            techDebt.DebtItems.Count(d => d.Priority >= 8));

        return new AgentResult
        {
            IsSuccess = true,
            TechDebt = techDebt,
            Content = response
        };
    }

    private string BuildPrompt(AgentContext context, string skillInstructions)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("Analyze the codebase for technical debt and provide a prioritized remediation plan:");
        sb.AppendLine();
        sb.AppendLine($"Application Name: {context.ApplicationName}");
        sb.AppendLine($"Tech Stack: {context.TechStack}");
        sb.AppendLine($"Description: {context.Description}");
        sb.AppendLine();
        
        if (context.GeneratedFiles != null && context.GeneratedFiles.Any())
        {
            sb.AppendLine("Codebase files:");
            foreach (var file in context.GeneratedFiles.Take(10))
            {
                sb.AppendLine($"- {file.RelativePath} ({file.Content.Length} bytes)");
                sb.AppendLine(file.Content.Substring(0, Math.Min(200, file.Content.Length)));
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("Please identify and prioritize:");
        sb.AppendLine("1. Code smells and anti-patterns");
        sb.AppendLine("2. Duplicate code");
        sb.AppendLine("3. Missing error handling");
        sb.AppendLine("4. Lack of tests");
        sb.AppendLine("5. Security vulnerabilities");
        sb.AppendLine("6. Performance issues");
        sb.AppendLine("7. Outdated dependencies");
        sb.AppendLine("8. Poor documentation");
        sb.AppendLine();
        sb.AppendLine("For each debt item, provide:");
        sb.AppendLine("- Description");
        sb.AppendLine("- Location (file/line)");
        sb.AppendLine("- Severity (1-10)");
        sb.AppendLine("- Estimated effort to fix");
        sb.AppendLine("- Priority (1-10)");
        sb.AppendLine("- Recommended action");

        return sb.ToString();
    }

    private TechDebtAnalysis ParseTechDebt(string content)
    {
        var analysis = new TechDebtAnalysis
        {
            DebtItems = new List<TechDebtItem>(),
            TrendAnalysis = new Dictionary<string, string>(),
            TotalDebtScore = 0
        };

        var lines = content.Split('\n');
        var currentItem = new TechDebtItem();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("##") || trimmed.StartsWith("#"))
            {
                if (!string.IsNullOrEmpty(currentItem.Description))
                {
                    analysis.DebtItems.Add(currentItem);
                    analysis.TotalDebtScore += currentItem.Severity;
                    currentItem = new TechDebtItem();
                }
            }
            else if (trimmed.Contains("severity") || trimmed.Contains("Severity"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"(\d+)");
                if (match.Success)
                {
                    currentItem.Severity = int.Parse(match.Groups[1].Value);
                }
            }
            else if (trimmed.Contains("priority") || trimmed.Contains("Priority"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"(\d+)");
                if (match.Success)
                {
                    currentItem.Priority = int.Parse(match.Groups[1].Value);
                }
            }
            else if (trimmed.StartsWith("-") || trimmed.StartsWith("*"))
            {
                if (string.IsNullOrEmpty(currentItem.Description))
                {
                    currentItem.Description = trimmed.TrimStart('-', '*').Trim();
                }
            }
        }

        if (!string.IsNullOrEmpty(currentItem.Description))
        {
            analysis.DebtItems.Add(currentItem);
            analysis.TotalDebtScore += currentItem.Severity;
        }

        return analysis;
    }
}

/// <summary>
/// Technical debt analysis
/// </summary>
public class TechDebtAnalysis
{
    public List<TechDebtItem> DebtItems { get; set; } = new();
    public Dictionary<string, string> TrendAnalysis { get; set; } = new();
    public int TotalDebtScore { get; set; }
    public string? RemediationPlan { get; set; }
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Technical debt item
/// </summary>
public class TechDebtItem
{
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int Severity { get; set; } // 1-10
    public int Priority { get; set; } // 1-10
    public string? EstimatedEffort { get; set; }
    public string? RecommendedAction { get; set; }
    public string? Category { get; set; } // code-smell, security, performance, etc.
}
