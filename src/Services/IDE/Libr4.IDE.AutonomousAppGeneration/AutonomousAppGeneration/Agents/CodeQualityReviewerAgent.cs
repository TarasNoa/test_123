using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Reviews code quality, security, performance, and best practices.
/// </summary>
public sealed class CodeQualityReviewerAgent : AgentSkillBase
{
    private readonly IAIService _aiService;
    private readonly ILogger _logger;

    public CodeQualityReviewerAgent(
        string skillPath,
        IAIService aiService,
        ILogger logger) : base(skillPath)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public override async Task<AgentResult> ExecuteAsync(AgentContext context)
    {
        _logger.LogInformation("CodeQualityReviewer reviewing task: {Description}", context.Task?.Description);

        var skillInstructions = GetSkillInstructions();
        var prompt = BuildReviewPrompt(context, skillInstructions);

        var response = await _aiService.GenerateCompletionAsync(prompt, skillInstructions);

        var (isApproved, feedback) = ParseReviewDecision(response);

        _logger.LogInformation(
            "Quality review result: Approved={Approved}, Feedback length={Length}",
            isApproved, feedback?.Length ?? 0);

        return new AgentResult
        {
            IsSuccess = isApproved,
            Content = response ?? string.Empty,
            Feedback = feedback
        };
    }

    private static string BuildReviewPrompt(AgentContext context, string skillInstructions)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## CODE QUALITY REVIEW");
        sb.AppendLine();
        sb.AppendLine($"Task: {context.Task?.Description}");
        sb.AppendLine($"Application: {context.ApplicationName}");
        sb.AppendLine($"Tech Stack: {context.TechStack}");

        if (!string.IsNullOrWhiteSpace(context.Feedback))
        {
            sb.AppendLine();
            sb.AppendLine("## CODE TO REVIEW");
            sb.AppendLine(context.Feedback);
        }

        sb.AppendLine();
        sb.AppendLine("## SKILL INSTRUCTIONS");
        sb.AppendLine(skillInstructions);

        sb.AppendLine();
        sb.AppendLine("## REQUIRED OUTPUT FORMAT");
        sb.AppendLine("VERDICT: APPROVED or NEEDS_FIX or REJECTED");
        sb.AppendLine("QUALITY_SCORE: [0-10]");
        sb.AppendLine("ISSUES: [list of critical/high/medium/low issues]");
        sb.AppendLine("FEEDBACK: [specific fixes with code examples]");

        return sb.ToString();
    }

    private static (bool approved, string? feedback) ParseReviewDecision(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return (false, "Empty review response");

        var lines = response.Split('\n');
        var verdictLine = lines.FirstOrDefault(l => l.Contains("VERDICT:", StringComparison.OrdinalIgnoreCase));

        var isApproved = verdictLine != null &&
                         verdictLine.Contains("APPROVED", StringComparison.OrdinalIgnoreCase) &&
                         !verdictLine.Contains("NEEDS_FIX", StringComparison.OrdinalIgnoreCase) &&
                         !verdictLine.Contains("REJECTED", StringComparison.OrdinalIgnoreCase);

        return (isApproved, response);
    }
}
