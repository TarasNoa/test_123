using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Observability agent with SLO designer, alert optimizer, and dashboard generator
/// Inspired by claude-skills observability-designer skill
/// </summary>
public class ObservabilityAgent : AgentSkillBase
{
    private readonly IAIService _aiService;
    private readonly ILogger _logger;

    public ObservabilityAgent(
        string skillPath,
        IAIService aiService,
        ILogger logger) : base(skillPath)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public override async Task<AgentResult> ExecuteAsync(AgentContext context)
    {
        _logger.LogInformation("Executing ObservabilityAgent for application: {ApplicationName}", context.ApplicationName);

        var skillInstructions = GetSkillInstructions();
        var prompt = BuildPrompt(context, skillInstructions);

        var response = await _aiService.GenerateCompletionAsync(prompt, skillInstructions);

        var observability = ParseObservability(response);

        _logger.LogInformation("Observability design completed with {SLOCount} SLOs, {AlertCount} alerts", 
            observability.SLOs.Count, 
            observability.Alerts.Count);

        return new AgentResult
        {
            IsSuccess = true,
            Observability = observability,
            Content = response
        };
    }

    private string BuildPrompt(AgentContext context, string skillInstructions)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("Design a comprehensive observability strategy for the following application:");
        sb.AppendLine();
        sb.AppendLine($"Application Name: {context.ApplicationName}");
        sb.AppendLine($"Tech Stack: {context.TechStack}");
        sb.AppendLine($"Description: {context.Description}");
        sb.AppendLine();
        
        if (context.GeneratedFiles != null && context.GeneratedFiles.Any())
        {
            sb.AppendLine("Key application components:");
            foreach (var file in context.GeneratedFiles.Where(f => 
                f.RelativePath.Contains("Controller") || 
                f.RelativePath.Contains("Service") ||
                f.RelativePath.Contains("API")))
            {
                sb.AppendLine($"- {file.RelativePath}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Please provide:");
        sb.AppendLine("1. Service Level Objectives (SLOs) with targets");
        sb.AppendLine("2. Key metrics to track (latency, error rate, throughput, etc.)");
        sb.AppendLine("3. Alert configuration with thresholds");
        sb.AppendLine("4. Dashboard layout and visualization");
        sb.AppendLine("5. Logging strategy");
        sb.AppendLine("6. Tracing implementation");
        sb.AppendLine("7. Recommended monitoring tools");

        return sb.ToString();
    }

    private ObservabilityDesign ParseObservability(string content)
    {
        var design = new ObservabilityDesign
        {
            SLOs = new List<ServiceLevelObjective>(),
            Alerts = new List<AlertConfiguration>(),
            Metrics = new List<string>(),
            DashboardConfig = new DashboardConfiguration()
        };

        var lines = content.Split('\n');
        var currentSection = string.Empty;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("##") || trimmed.StartsWith("#"))
            {
                currentSection = trimmed.Trim('#').Trim().ToLower();
            }
            else if (currentSection.Contains("slo") || currentSection.Contains("objective"))
            {
                if (!string.IsNullOrEmpty(trimmed) && (trimmed.StartsWith("-") || trimmed.StartsWith("*")))
                {
                    var slo = new ServiceLevelObjective
                    {
                        Description = trimmed.TrimStart('-', '*').Trim()
                    };
                    design.SLOs.Add(slo);
                }
            }
            else if (currentSection.Contains("alert"))
            {
                if (!string.IsNullOrEmpty(trimmed) && (trimmed.StartsWith("-") || trimmed.StartsWith("*")))
                {
                    var alert = new AlertConfiguration
                    {
                        Description = trimmed.TrimStart('-', '*').Trim()
                    };
                    design.Alerts.Add(alert);
                }
            }
            else if (currentSection.Contains("metric"))
            {
                if (!string.IsNullOrEmpty(trimmed) && (trimmed.StartsWith("-") || trimmed.StartsWith("*")))
                {
                    design.Metrics.Add(trimmed.TrimStart('-', '*').Trim());
                }
            }
        }

        return design;
    }
}

/// <summary>
/// Observability design structure
/// </summary>
public class ObservabilityDesign
{
    public List<ServiceLevelObjective> SLOs { get; set; } = new();
    public List<AlertConfiguration> Alerts { get; set; } = new();
    public List<string> Metrics { get; set; } = new();
    public DashboardConfiguration DashboardConfig { get; set; } = new();
    public string? LoggingStrategy { get; set; }
    public string? TracingImplementation { get; set; }
    public List<string> RecommendedTools { get; set; } = new();
}

/// <summary>
/// Service Level Objective
/// </summary>
public class ServiceLevelObjective
{
    public string Description { get; set; } = string.Empty;
    public string? MetricName { get; set; }
    public double Target { get; set; }
    public string? TimeWindow { get; set; }
    public string? ErrorBudget { get; set; }
}

/// <summary>
/// Alert configuration
/// </summary>
public class AlertConfiguration
{
    public string Description { get; set; } = string.Empty;
    public string? MetricName { get; set; }
    public double Threshold { get; set; }
    public string? Condition { get; set; }
    public string? Severity { get; set; }
    public string? NotificationChannel { get; set; }
}

/// <summary>
/// Dashboard configuration
/// </summary>
public class DashboardConfiguration
{
    public string? Name { get; set; }
    public List<DashboardPanel> Panels { get; set; } = new();
    public string? RefreshInterval { get; set; }
}

/// <summary>
/// Dashboard panel
/// </summary>
public class DashboardPanel
{
    public string? Title { get; set; }
    public string? Type { get; set; }
    public string? Query { get; set; }
    public string? Visualization { get; set; }
}
