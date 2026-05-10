using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Domain.Agents.AgentHierarchy;

public class CodeReviewerAgent : BaseAgent
{
    private readonly ICodeAnalysisService _codeAnalyzer;

    public CodeReviewerAgent(ILogger<BaseAgent> logger, ICodeAnalysisService codeAnalyzer)
        : base(logger, "CodeReviewerAgent", AgentType.CodeReviewer)
    {
        _codeAnalyzer = codeAnalyzer;
    }

    protected override async Task<string> ExecuteInternalAsync(AgentRequest request)
    {
        _logger.LogInformation($"CodeReviewer analyzing: {request.Task}");

        var code = request.Parameters.ContainsKey("code")
            ? request.Parameters["code"].ToString() ?? ""
            : "";

        if (string.IsNullOrEmpty(code))
        {
            return "No code provided for review";
        }

        var analysis = await _codeAnalyzer.AnalyzeCodeAsync(code);

        var review = $@"
Code Review Analysis:
=====================
Issues: {analysis.IssueCount}
Quality Score: {analysis.QualityScore}/100
Performance Issues: {analysis.PerformanceIssues}

Recommendations:
{string.Join("\n", analysis.Recommendations)}

Security Concerns:
{string.Join("\n", analysis.SecurityConcerns)}
";

        return review;
    }

    public override Task<bool> CanHandleAsync(string taskType)
    {
        var canHandle = taskType.Contains("review", StringComparison.OrdinalIgnoreCase) ||
                       taskType.Contains("analyze", StringComparison.OrdinalIgnoreCase) ||
                       taskType.Contains("quality", StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(canHandle);
    }

    public override AgentCapabilities GetCapabilities()
    {
        return new AgentCapabilities
        {
            SupportedTasks = new List<string>
            {
                "code review",
                "analyze code",
                "check quality",
                "security audit",
                "performance review"
            },
            SupportedLanguages = new List<string> { "csharp", "fsharp", "typescript", "python" },
            MaxConcurrentTasks = 8,
            AverageExecutionTime = TimeSpan.FromSeconds(2),
            SuccessRate = 0.92
        };
    }
}