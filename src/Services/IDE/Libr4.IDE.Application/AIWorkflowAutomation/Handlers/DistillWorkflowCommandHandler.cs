using Libr4.IDE.Application.AIWorkflowAutomation.Commands;
using Libr4.IDE.Application.AIWorkflowAutomation.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AIWorkflowAutomation.Handlers;

/// <summary>
/// Distills a workflow into reusable patterns and extracted skills by analyzing step sequences.
/// </summary>
public class DistillWorkflowCommandHandler : IRequestHandler<DistillWorkflowCommand, WorkflowAnalysisDto>
{
    private readonly ILogger<DistillWorkflowCommandHandler> _logger;

    public DistillWorkflowCommandHandler(ILogger<DistillWorkflowCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task<WorkflowAnalysisDto> Handle(DistillWorkflowCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Distilling workflow {WorkflowId} ({StepCount} steps)",
            request.WorkflowId, request.WorkflowSteps.Count);

        var patterns = ExtractPatterns(request.WorkflowSteps);
        var skills = ExtractSkills(request.WorkflowSteps);

        var result = new WorkflowAnalysisDto
        {
            Id = Guid.NewGuid(),
            AnalysisId = $"analysis-{Guid.NewGuid():N}"[..20],
            WorkflowId = request.WorkflowId,
            Patterns = patterns,
            ExtractedSkills = skills,
            Status = "Completed",
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }

    private static List<WorkflowPatternDto> ExtractPatterns(IReadOnlyList<string> steps)
    {
        var patterns = new List<WorkflowPatternDto>();
        if (steps.Count == 0) return patterns;

        var verbs = new[] { "build", "test", "deploy", "fetch", "parse", "validate", "send", "read", "write" };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in steps)
        {
            var lower = step.ToLowerInvariant();
            foreach (var verb in verbs)
            {
                if (lower.Contains(verb) && seen.Add(verb))
                {
                    patterns.Add(new WorkflowPatternDto
                    {
                        Id = Guid.NewGuid(),
                        PatternName = $"{char.ToUpperInvariant(verb[0])}{verb[1..]}Pattern",
                        Description = $"Pattern detected from step: {step}",
                        Steps = steps.Where(s => s.Contains(verb, StringComparison.OrdinalIgnoreCase)).ToList(),
                        Frequency = steps.Count(s => s.Contains(verb, StringComparison.OrdinalIgnoreCase))
                    });
                }
            }
        }

        return patterns;
    }

    private static List<ExtractedSkillDto> ExtractSkills(IReadOnlyList<string> steps)
    {
        var skills = new List<ExtractedSkillDto>();
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step.Length < 4) continue;
            skills.Add(new ExtractedSkillDto
            {
                Id = Guid.NewGuid(),
                SkillName = step.Length > 40 ? step[..40] : step,
                Description = step,
                ConfidenceScore = 0.7 + (i % 3) * 0.1
            });
        }
        return skills;
    }
}
