/*
using MediatR;
using Libr4.IDE.Application.ArchitecturalGuardrails.Commands;
using Libr4.IDE.Application.ArchitecturalGuardrails.DTOs;
using Libr4.IDE.Domain.ArchitecturalGuardrails;
using Libr4.AI.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.ArchitecturalGuardrails.Handlers;

/// <summary>
/// Handler for RunValidationCommand - Validates code against architectural rules
/// </summary>
public class RunValidationCommandHandler : IRequestHandler<RunValidationCommand, ArchitectureValidationDto>
{
    private readonly IAIService _aiService;
    private readonly ILogger<RunValidationCommandHandler> _logger;

    public RunValidationCommandHandler(IAIService aiService, ILogger<RunValidationCommandHandler> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<ArchitectureValidationDto> Handle(RunValidationCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Running architecture validation for {FileCount} files", request.Files.Count);

        var violations = new List<ArchitectureViolationDto>();

        // Default architectural rules
        var rules = new List<GuardrailRule>
        {
            new() { Name = "NoCircularDependencies", Severity = GuardrailSeverity.Error, Category = "Coupling" },
            new() { Name = "LayeredArchitecture", Severity = GuardrailSeverity.Error, Category = "Structure" },
            new() { Name = "SingleResponsibility", Severity = GuardrailSeverity.Warning, Category = "SOLID" },
            new() { Name = "DependencyInversion", Severity = GuardrailSeverity.Warning, Category = "SOLID" },
            new() { Name = "NoGodClasses", Severity = GuardrailSeverity.Error, Category = "Complexity" }
        };

        // Add custom rules
        rules.AddRange(request.CustomRules);

        foreach (var (filePath, content) in request.Files)
        {
            var fileViolations = ValidateFile(filePath, content, rules);
            violations.AddRange(fileViolations);
        }

        // AI-enhanced validation
        var aiViolations = await ValidateWithAIAsync(request.Files, rules, ct);
        violations.AddRange(aiViolations);

        var passed = violations.All(v => v.Severity != GuardrailSeverity.Error);

        return new ArchitectureValidationDto
        {
            Id = Guid.NewGuid(),
            WorkspaceId = request.WorkspaceId,
            Passed = passed,
            Violations = violations,
            Summary = $"{violations.Count(v => v.Severity == GuardrailSeverity.Error)} errors, {violations.Count(v => v.Severity == GuardrailSeverity.Warning)} warnings",
            ValidatedAt = DateTime.UtcNow
        };
    }

    private List<ArchitectureViolationDto> ValidateFile(string filePath, string content, List<GuardrailRule> rules)
    {
        var violations = new List<ArchitectureViolationDto>();
        var lines = content.Split('\n');

        foreach (var rule in rules)
        {
            switch (rule.Name)
            {
                case "NoGodClasses":
                    var classMatch = Regex.Match(content, @"class\s+\w+");
                    if (classMatch.Success)
                    {
                        var methodCount = Regex.Matches(content, @"(public|private|protected)\s+\w+\s+\w+\s*\(").Count;
                        if (methodCount > 20)
                        {
                            violations.Add(new ArchitectureViolationDto
                            {
                                Id = Guid.NewGuid(),
                                RuleName = rule.Name,
                                Severity = rule.Severity,
                                Category = rule.Category,
                                FilePath = filePath,
                                LineNumber = 1,
                                Message = $"Class has {methodCount} methods (threshold: 20). Consider splitting.",
                                Suggestion = "Apply Single Responsibility Principle - split into smaller classes"
                            });
                        }
                    }
                    break;

                case "SingleResponsibility":
                    if (content.Contains("public class Service") && content.Contains("Repository") && content.Contains("Controller"))
                    {
                        violations.Add(new ArchitectureViolationDto
                        {
                            Id = Guid.NewGuid(),
                            RuleName = rule.Name,
                            Severity = rule.Severity,
                            Category = rule.Category,
                            FilePath = filePath,
                            Message = "Class mixes multiple responsibilities (Service + Repository + Controller)",
                            Suggestion = "Separate concerns into distinct classes"
                        });
                    }
                    break;

                case "LayeredArchitecture":
                    if (filePath.Contains("Domain") && content.Contains("HttpClient"))
                    {
                        violations.Add(new ArchitectureViolationDto
                        {
                            Id = Guid.NewGuid(),
                            RuleName = rule.Name,
                            Severity = rule.Severity,
                            Category = rule.Category,
                            FilePath = filePath,
                            Message = "Domain layer depends on infrastructure (HttpClient)",
                            Suggestion = "Move HTTP concerns to Infrastructure layer"
                        });
                    }
                    break;
            }
        }

        return violations;
    }

    private async Task<List<ArchitectureViolationDto>> ValidateWithAIAsync(
        List<(string FilePath, string Content)> files,
        List<GuardrailRule> rules,
        CancellationToken ct)
    {
        var violations = new List<ArchitectureViolationDto>();

        try
        {
            var fileSummaries = files.Select(f => $"{f.FilePath}: {f.Content.Length} chars").Take(5);
            var prompt = $@"
Analyze these files for architectural issues:
{string.Join("\n", fileSummaries)}

Focus on: Clean Architecture, SOLID principles, DDD patterns.
Return violations as: RuleName|Severity|File|Message
Example: DependencyInversion|Warning|Service.cs|Service directly instantiates Repository";

            var response = await _aiService.GenerateCompletionAsync(prompt, cancellationToken: ct);

            foreach (var line in response.Split('\n').Where(l => l.Contains('|')))
            {
                var parts = line.Split('|');
                if (parts.Length >= 4)
                {
                    if (Enum.TryParse<GuardrailSeverity>(parts[1], out var severity))
                    {
                        violations.Add(new ArchitectureViolationDto
                        {
                            Id = Guid.NewGuid(),
                            RuleName = parts[0].Trim(),
                            Severity = severity,
                            FilePath = parts[2].Trim(),
                            Message = parts[3].Trim(),
                            Source = "AI"
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI validation failed");
        }

        return violations;
    }
}
*/
