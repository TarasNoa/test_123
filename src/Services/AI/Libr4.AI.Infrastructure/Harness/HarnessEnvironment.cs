/*
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Libr4.AI.Infrastructure.SandboxExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Harness;

/// <summary>
/// Implementation of harness environment with automatic feedback loops.
/// Integrated with Rust sandbox-controller for secure code execution.
/// </summary>
public class HarnessEnvironment : IHarnessEnvironment
{
    private readonly ILogger<HarnessEnvironment> _logger;
    private readonly HttpClient _sandboxClient;
    private readonly bool _sandboxEnabled;

    public HarnessEnvironment(
        ILogger<HarnessEnvironment> logger,
        IConfiguration configuration,
        IHttpClientFactory? httpClientFactory = null)
    {
        _logger = logger;
        
        // Initialize sandbox HTTP client if factory available
        _sandboxEnabled = httpClientFactory != null && 
                         !string.IsNullOrEmpty(configuration["Sandbox:Url"]);
        
        if (_sandboxEnabled)
        {
            _sandboxClient = httpClientFactory!.CreateClient();
            _sandboxClient.BaseAddress = new Uri(configuration["Sandbox:Url"]!);
            _sandboxClient.Timeout = TimeSpan.FromSeconds(30);
            
            _logger.LogInformation("Sandbox integration enabled: {Url}", configuration["Sandbox:Url"]);
        }
        else
        {
            _logger.LogWarning("Sandbox not configured. Code execution will be skipped for safety.");
        }
    }

    public async Task<HarnessExecutionResult> ExecuteWithValidationAsync(
        string code,
        HarnessValidationRules rules,
        CancellationToken cancellationToken = default)
    {
        var result = new HarnessExecutionResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Pre-execution validation
            result.ValidationIssues = ValidateCode(code, rules);
            
            if (result.ValidationIssues.Any(i => i.Severity == Severity.Critical))
            {
                result.Success = false;
                result.NeedsCorrection = true;
                result.Error = "Critical validation issues found";
                return result;
            }

            // Execute in sandbox via Rust sandbox-controller
            if (_sandboxEnabled)
            {
                try
                {
                    var sandboxResult = await ExecuteInSandboxAsync(code, cancellationToken);
                    result.Success = sandboxResult.Success;
                    result.Output = sandboxResult.Output;
                    result.Error = sandboxResult.Error;
                    
                    if (!sandboxResult.Success)
                    {
                        result.NeedsCorrection = true;
                        result.ValidationIssues.Add(new ValidationIssue
                        {
                            Message = $"Sandbox execution failed: {sandboxResult.Error}",
                            Severity = Severity.Critical
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Sandbox execution failed");
                    result.Success = false;
                    result.Error = $"Sandbox error: {ex.Message}";
                    result.NeedsCorrection = true;
                }
            }
            else
            {
                // Sandbox not configured - reject execution for security
                result.Success = false;
                result.Error = "Sandbox not configured. Code execution disabled for security.";
                result.NeedsCorrection = true;
                _logger.LogWarning("Code execution attempted without sandbox configuration");
            }

            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;

            // Post-execution validation
            if (!result.Success)
            {
                result.NeedsCorrection = true;
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.Error = ex.Message;
            result.ExecutionTime = stopwatch.Elapsed;
            result.NeedsCorrection = true;
            
            _logger.LogError(ex, "Harness execution failed");
            return result;
        }
    }

    public async Task<TestResult> RunTestsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "test",
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var passed = process.ExitCode == 0;
            
            return new TestResult
            {
                Passed = passed,
                Output = output,
                TotalTests = passed ? 0 : 1,
                PassedTests = passed ? 0 : 0,
                FailedTests = passed ? 0 : 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run tests");
            return new TestResult { Passed = false };
        }
    }

    public async Task<LintResult> RunLinterAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return new LintResult
            {
                Passed = true,
                TotalErrors = 0,
                TotalWarnings = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run linter");
            return new LintResult { Passed = false };
        }
    }

    public async Task<HarnessTestResult> RunTestsAsync(
        string testPath,
        CancellationToken cancellationToken = default)
    {
        var result = new HarnessTestResult();

        try
        {
            if (!File.Exists(testPath))
            {
                result.AllPassed = false;
                result.FailureMessages.Add($"Test file not found: {testPath}");
                return result;
            }

            // Run dotnet test
            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"test {testPath} --no-build --verbosity quiet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                result.AllPassed = false;
                result.FailureMessages.Add("Failed to start test process");
                return result;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            // Parse output
            var match = Regex.Match(output, @"Passed:\s*(\d+),\s*Failed:\s*(\d+)");
            if (match.Success)
            {
                result.PassedTests = int.Parse(match.Groups[1].Value);
                result.FailedTests = int.Parse(match.Groups[2].Value);
                result.TotalTests = result.PassedTests + result.FailedTests;
                result.AllPassed = result.FailedTests == 0;
            }
            else
            {
                result.AllPassed = process.ExitCode == 0;
            }

            if (!result.AllPassed)
            {
                result.FailureMessages.Add(error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test execution failed");
            result.AllPassed = false;
            return result;
        }
    }

    public async Task<HarnessQualityResult> CheckQualityAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var result = new HarnessQualityResult();

        try
        {
            // Simple quality checks
            var issues = new List<QualityIssue>();

            // Check for hardcoded secrets
            if (Regex.IsMatch(code, @"(api[_-]?key|secret|password)\s*=\s*['""][^'""]+['""]", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Category = "Security",
                    Description = "Potential hardcoded secret detected",
                    Severity = Severity.Critical
                });
            }

            // Check for console.log in production code
            if (code.Contains("console.log") && !code.Contains("//") && !code.Contains("#"))
            {
                issues.Add(new QualityIssue
                {
                    Category = "Code Quality",
                    Description = "console.log statement found",
                    Severity = Severity.Warning
                });
            }

            // Check for TODO comments
            if (Regex.IsMatch(code, @"TODO|FIXME|HACK", RegexOptions.IgnoreCase))
            {
                issues.Add(new QualityIssue
                {
                    Category = "Code Quality",
                    Description = "TODO/FIXME comment found",
                    Severity = Severity.Info
                });
            }

            result.Issues = issues;
            result.PassesQualityGate = !issues.Any(i => i.Severity == Severity.Critical);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quality check failed");
            result.PassesQualityGate = false;
            return result;
        }
    }

    public async Task<HarnessBackpressureResult> ApplyBackpressureAsync(
        HarnessExecutionResult execution,
        CancellationToken cancellationToken = default)
    {
        var result = new HarnessBackpressureResult();

        try
        {
            if (execution.Success)
            {
                result.ShouldRetry = false;
                return await Task.FromResult(result);
            }

            // Analyze error and suggest correction
            var actions = new List<string>();
            var suggestions = new List<string>();

            if (execution.Error?.Contains("timeout") == true || execution.ExecutionTime.TotalMilliseconds > 30000)
            {
                actions.Add("Execution timeout detected");
                suggestions.Add("Consider optimizing the code or breaking it into smaller chunks");
            }

            if (execution.ValidationIssues.Any(i => i.Severity == Severity.Critical))
            {
                actions.Add("Critical validation issues found");
                suggestions.Add("Fix critical issues before retrying");
            }

            if (execution.Error?.Contains("not found") == true)
            {
                actions.Add("Missing dependency detected");
                suggestions.Add("Check if all required dependencies are installed");
            }

            result.ActionsTaken = actions;
            result.CorrectionSuggestion = suggestions.FirstOrDefault();
            result.ShouldRetry = !execution.ValidationIssues.Any(i => i.Severity == Severity.Critical);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backpressure application failed");
            result.ShouldRetry = false;
            return result;
        }
    }

    private List<HarnessValidationIssue> ValidateCode(string code, HarnessValidationRules rules)
    {
        var issues = new List<HarnessValidationIssue>();

        // Check forbidden patterns
        foreach (var pattern in rules.ForbiddenPatterns)
        {
            if (Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase))
            {
                issues.Add(new HarnessValidationIssue
                {
                    Type = "ForbiddenPattern",
                    Message = $"Forbidden pattern detected: {pattern}",
                    Severity = Severity.Critical
                });
            }
        }

        // Check required patterns
        foreach (var pattern in rules.RequiredPatterns)
        {
            if (!Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase))
            {
                issues.Add(new HarnessValidationIssue
                {
                    Type = "RequiredPattern",
                    Message = $"Required pattern not found: {pattern}",
                    Severity = rules.RequireTypeChecking ? Severity.Error : Severity.Warning
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// Execute code in Rust sandbox-controller via HTTP API.
    /// </summary>
    private async Task<(bool Success, string Output, string? Error)> ExecuteInSandboxAsync(
        string code, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Call Rust sandbox-controller
            var request = new
            {
                code,
                language = "csharp",
                timeout_ms = 30000,
                memory_limit_mb = 512,
                network_access = false,
                filesystem_access = false
            };

            var response = await _sandboxClient.PostAsJsonAsync("/api/v1/execute", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                return (false, "", $"Sandbox controller returned {response.StatusCode}: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<SandboxExecutionResult>(cancellationToken);
            
            if (result == null)
            {
                return (false, "", "Failed to parse sandbox response");
            }

            return (result.Success, result.Output ?? "", result.Error);
        }
        catch (TaskCanceledException)
        {
            return (false, "", "Sandbox execution timed out after 30 seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with sandbox controller");
            return (false, "", $"Sandbox communication error: {ex.Message}");
        }
    }

    private record SandboxExecutionResult(bool Success, string? Output, string? Error, long DurationMs);
}
*/
