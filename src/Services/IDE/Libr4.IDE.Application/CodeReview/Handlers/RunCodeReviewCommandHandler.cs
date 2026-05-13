/*
using MediatR;
using Libr4.IDE.Application.CodeReview.Commands;
using Libr4.IDE.Application.CodeReview.DTOs;
using Libr4.IDE.Domain.CodeReview;
using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.CodeReview.Handlers;

/// <summary>
/// Handler for RunCodeReviewCommand - AI-powered code review without F# dependency
/// </summary>
public class RunCodeReviewCommandHandler : IRequestHandler<RunCodeReviewCommand, CodeReviewDto>
{
    private readonly IAIService _aiService;
    private readonly ILogger<RunCodeReviewCommandHandler> _logger;

    public RunCodeReviewCommandHandler(
        IAIService aiService,
        ILogger<RunCodeReviewCommandHandler> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<CodeReviewDto> Handle(RunCodeReviewCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Starting code review for workspace {WorkspaceId}", request.WorkspaceId);
        
        var reviewId = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;
        
        // Analyze each file
        var allIssues = new List<ReviewIssueDto>();
        foreach (var file in request.Files)
        {
            var issues = await AnalyzeFileAsync(file, request.ReviewTypes, ct);
            allIssues.AddRange(issues);
        }
        
        _logger.LogInformation("Code review completed: {IssueCount} issues found", allIssues.Count);
        
        return new CodeReviewDto
        {
            Id = Guid.NewGuid(),
            ReviewId = reviewId,
            WorkspaceId = request.WorkspaceId,
            Files = request.Files,
            Issues = allIssues,
            Status = "Completed",
            StartedAt = startedAt,
            CompletedAt = DateTime.UtcNow
        };
    }

    private async Task<List<ReviewIssueDto>> AnalyzeFileAsync(
        string filePath, 
        List<ReviewType> reviewTypes, 
        CancellationToken ct)
    {
        var issues = new List<ReviewIssueDto>();
        
        try
        {
            // Read file content (in real scenario, from workspace)
            var content = await File.ReadAllTextAsync(filePath, ct);
            
            // Pattern-based analysis (fast)
            issues.AddRange(AnalyzePatterns(filePath, content));
            
            // AI-based analysis for complex issues (if AI service available)
            if (reviewTypes.Contains(ReviewType.AiAnalysis))
            {
                var aiIssues = await AnalyzeWithAIAsync(filePath, content, ct);
                issues.AddRange(aiIssues);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze file {FilePath}", filePath);
        }
        
        return issues;
    }

    private List<ReviewIssueDto> AnalyzePatterns(string filePath, string content)
    {
        var issues = new List<ReviewIssueDto>();
        var lines = content.Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;
            
            // TODO pattern
            if (line.Contains("// TODO") || line.Contains("// FIXME"))
            {
                issues.Add(new ReviewIssueDto
                {
                    Id = Guid.NewGuid(),
                    FilePath = filePath,
                    LineNumber = lineNumber,
                    Severity = "Warning",
                    Category = "Maintainability",
                    Message = "TODO/FIXME comment found",
                    Suggestion = "Resolve before merging"
                });
            }
            
            // Hardcoded secrets pattern
            if (line.Contains("password") || line.Contains("secret") || line.Contains("apikey"))
            {
                if (line.Contains("=") && !line.Contains("Environment"))
                {
                    issues.Add(new ReviewIssueDto
                    {
                        Id = Guid.NewGuid(),
                        FilePath = filePath,
                        LineNumber = lineNumber,
                        Severity = "Critical",
                        Category = "Security",
                        Message = "Potential hardcoded secret",
                        Suggestion = "Use environment variables or secret management"
                    });
                }
            }
            
            // Exception swallowing
            if (line.Contains("catch") && line.Contains("{ }"))
            {
                issues.Add(new ReviewIssueDto
                {
                    Id = Guid.NewGuid(),
                    FilePath = filePath,
                    LineNumber = lineNumber,
                    Severity = "Error",
                    Category = "Reliability",
                    Message = "Empty catch block",
                    Suggestion = "Log exception or handle properly"
                });
            }
        }
        
        return issues;
    }

    private async Task<List<ReviewIssueDto>> AnalyzeWithAIAsync(
        string filePath, 
        string content, 
        CancellationToken ct)
    {
        var issues = new List<ReviewIssueDto>();
        
        try
        {
            var prompt = $"""
                Review this code for issues:
                
                File: {filePath}
                ```
                {content.Substring(0, Math.Min(content.Length, 2000))}
                ```
                
                Identify: bugs, security issues, performance problems, best practice violations.
                Return JSON array of issues with fields: severity, category, message, lineNumber.
                """;
            
            var response = await _aiService.GenerateCompletionAsync(prompt, cancellationToken: ct);
            
            // Parse AI response (simplified)
            if (response.Contains("issue") || response.Contains("problem"))
            {
                issues.Add(new ReviewIssueDto
                {
                    Id = Guid.NewGuid(),
                    FilePath = filePath,
                    LineNumber = 0,
                    Severity = "Info",
                    Category = "AIReview",
                    Message = "AI suggests code review: " + response.Substring(0, Math.Min(100, response.Length)),
                    Suggestion = "Review AI suggestions"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI analysis failed for {FilePath}", filePath);
        }
        
        return issues;
    }
}
*/
