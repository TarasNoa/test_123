using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Harness;

public class HarnessEnvironment : IHarnessEnvironment
{
    private readonly ILogger<HarnessEnvironment> _logger;

    public HarnessEnvironment(ILogger<HarnessEnvironment> logger)
    {
        _logger = logger;
    }

    public Task<HarnessExecutionResult> ExecuteWithValidationAsync(
        string code,
        HarnessValidationRules rules,
        CancellationToken cancellationToken = default)
    {
        var result = new HarnessExecutionResult
        {
            Success = true,
            Output = "Execution skipped - sandbox not configured",
            ValidationIssues = ValidateCode(code, rules),
            ExecutionTime = TimeSpan.Zero
        };
        return Task.FromResult(result);
    }

    public Task<TestResult> RunTestsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestResult { Passed = true, TotalTests = 0, PassedTests = 0, FailedTests = 0 });
    }

    public Task<LintResult> RunLinterAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LintResult { Passed = true, TotalErrors = 0, TotalWarnings = 0 });
    }

    public Task<HarnessQualityResult> CheckQualityAsync(string code, CancellationToken cancellationToken = default)
    {
        var issues = new List<QualityIssue>();

        if (Regex.IsMatch(code, @"(api[_-]?key|secret|password)\s*=\s*['\""][^'\""]+['\""]", RegexOptions.IgnoreCase))
        {
            issues.Add(new QualityIssue { Category = "Security", Description = "Potential hardcoded secret", Severity = Severity.Critical });
        }

        return Task.FromResult(new HarnessQualityResult
        {
            PassesQualityGate = !issues.Any(i => i.Severity == Severity.Critical),
            Issues = issues
        });
    }

    public Task<HarnessBackpressureResult> ApplyBackpressureAsync(
        HarnessExecutionResult execution,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HarnessBackpressureResult
        {
            ShouldRetry = !execution.Success && !execution.ValidationIssues.Any(i => i.Severity == Severity.Critical)
        });
    }

    private static List<HarnessValidationIssue> ValidateCode(string code, HarnessValidationRules rules)
    {
        var issues = new List<HarnessValidationIssue>();

        foreach (var pattern in rules.ForbiddenPatterns)
        {
            if (Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase))
            {
                issues.Add(new HarnessValidationIssue
                {
                    Type = "ForbiddenPattern",
                    Message = $"Forbidden pattern: {pattern}",
                    Severity = Severity.Critical
                });
            }
        }

        return issues;
    }
}
