/*
using MediatR;
using Libr4.IDE.Application.HackerAgent.Commands;
using Libr4.IDE.Application.HackerAgent.DTOs;
using Libr4.IDE.Domain.HackerAgent;
using Libr4.AI.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Libr4.IDE.Application.HackerAgent.Handlers;

/// <summary>
/// Handler for RunHackerAgentCommand - Professional security testing agent
/// </summary>
public class RunHackerAgentCommandHandler : IRequestHandler<RunHackerAgentCommand, HackerAgentDto>
{
    private readonly IAIService _aiService;
    private readonly ILogger<RunHackerAgentCommandHandler> _logger;

    public RunHackerAgentCommandHandler(
        IAIService aiService,
        ILogger<RunHackerAgentCommandHandler> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<HackerAgentDto> Handle(RunHackerAgentCommand request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Starting hacker agent operation for workspace {WorkspaceId}, target {Target}",
            request.WorkspaceId, request.Target);

        var operationId = Guid.NewGuid().ToString("N")[..8];
        var agent = HackerAgent.Create(operationId, request.WorkspaceId);

        try
        {
            // Phase 1: Generate security scripts based on target
            var scripts = await GenerateSecurityScriptsAsync(request.Target, request.ScriptType, ct);
            foreach (var script in scripts)
            {
                agent.AddScript(script);
                agent.MarkScriptGenerated(script);
            }

            // Phase 2: Fetch relevant GitHub security tools
            var tools = await FetchSecurityToolsAsync(request.Target, ct);
            foreach (var tool in tools)
            {
                agent.AddTool(tool);
                agent.MarkToolFetched(tool);
            }

            // Phase 3: Execute security tests
            agent.SetStatus("running");
            var results = await ExecuteSecurityTestsAsync(agent, request.Target, ct);
            foreach (var result in results)
            {
                agent.AddTestResult(result);
            }

            // Complete operation
            agent.MarkSecurityTestCompleted();
            agent.SetStatus("completed");

            _logger.LogInformation(
                "Hacker agent operation {OperationId} completed. Scripts: {ScriptCount}, Tools: {ToolCount}, Results: {ResultCount}",
                operationId, scripts.Count, tools.Count, results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hacker agent operation {OperationId} failed", operationId);
            agent.SetStatus("failed");
            agent.AddTestResult($"ERROR: {ex.Message}");
        }

        return MapToDto(agent);
    }

    private async Task<List<SecurityScript>> GenerateSecurityScriptsAsync(
        string target, ScriptType scriptType, CancellationToken ct)
    {
        var scripts = new List<SecurityScript>();

        // Generate reconnaissance script
        var reconScript = await GenerateScriptAsync(
            target,
            scriptType,
            "reconnaissance",
            "Perform initial reconnaissance and information gathering",
            ct);
        scripts.Add(reconScript);

        // Generate vulnerability scan script
        var vulnScript = await GenerateScriptAsync(
            target,
            scriptType,
            "vulnerability_scan",
            "Scan for known vulnerabilities (OWASP Top 10, CVEs)",
            ct);
        scripts.Add(vulnScript);

        // Generate penetration test script
        var pentestScript = await GenerateScriptAsync(
            target,
            scriptType,
            "penetration_test",
            "Attempt controlled penetration testing",
            ct);
        scripts.Add(pentestScript);

        // Generate report generation script
        var reportScript = await GenerateScriptAsync(
            target,
            scriptType,
            "report_generator",
            "Generate comprehensive security report",
            ct);
        scripts.Add(reportScript);

        return scripts;
    }

    private async Task<SecurityScript> GenerateScriptAsync(
        string target,
        ScriptType scriptType,
        string scriptName,
        string description,
        CancellationToken ct)
    {
        var prompt = $@"
Generate a professional security testing script for:
- Target: {target}
- Script Type: {scriptType}
- Purpose: {description}
- Script Name: {scriptName}

The script should:
1. Include proper error handling
2. Log all activities
3. Follow security best practices
4. Generate structured output
5. Be production-ready

Return the complete script code.";

        var code = await _aiService.GenerateCompletionAsync(prompt, cancellationToken: ct);

        return new SecurityScript
        {
            Id = Guid.NewGuid(),
            ScriptName = $"{scriptName}.{GetScriptExtension(scriptType)}",
            Code = code,
            Language = scriptType.ToString(),
            Purpose = description,
            CreatedAt = DateTime.UtcNow
        };
    }

    private string GetScriptExtension(ScriptType type) => type switch
    {
        ScriptType.Python => "py",
        ScriptType.Bash => "sh",
        ScriptType.PowerShell => "ps1",
        ScriptType.Ruby => "rb",
        _ => "txt"
    };

    private async Task<List<GitHubSecurityTool>> FetchSecurityToolsAsync(string target, CancellationToken ct)
    {
        var tools = new List<GitHubSecurityTool>();

        // Use AI to determine relevant tools based on target
        var prompt = $@"
Analyze this target and recommend 3-5 specific GitHub security tools:
Target: {target}

For each tool provide:
- Repository name (format: owner/repo)
- Why it's relevant
- Key features to use

Format: owner/repo|reason|features";

        var response = await _aiService.GenerateCompletionAsync(prompt, cancellationToken: ct);

        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length >= 1 && parts[0].Contains('/'))
            {
                var repoParts = parts[0].Trim().Split('/');
                if (repoParts.Length == 2)
                {
                    tools.Add(new GitHubSecurityTool
                    {
                        Id = Guid.NewGuid(),
                        RepoName = parts[0].Trim(),
                        Owner = repoParts[0],
                        Repo = repoParts[1],
                        Description = parts.Length > 1 ? parts[1].Trim() : "Security testing tool",
                        FetchedAt = DateTime.UtcNow,
                        IsCloned = false
                    });
                }
            }
        }

        // Add default tools if AI didn't return enough
        if (tools.Count < 3)
        {
            tools.AddRange(GetDefaultSecurityTools());
        }

        return tools.Take(5).ToList();
    }

    private List<GitHubSecurityTool> GetDefaultSecurityTools() => new()
    {
        new GitHubSecurityTool
        {
            Id = Guid.NewGuid(),
            RepoName = "OWASP/ZAP",
            Owner = "OWASP",
            Repo = "ZAP",
            Description = "OWASP Zed Attack Proxy - Web app scanner",
            FetchedAt = DateTime.UtcNow
        },
        new GitHubSecurityTool
        {
            Id = Guid.NewGuid(),
            RepoName = "sqlmapproject/sqlmap",
            Owner = "sqlmapproject",
            Repo = "sqlmap",
            Description = "Automatic SQL injection and database takeover tool",
            FetchedAt = DateTime.UtcNow
        },
        new GitHubSecurityTool
        {
            Id = Guid.NewGuid(),
            RepoName = "nmap/nmap",
            Owner = "nmap",
            Repo = "nmap",
            Description = "Network discovery and security auditing",
            FetchedAt = DateTime.UtcNow
        }
    };

    private async Task<List<string>> ExecuteSecurityTestsAsync(
        HackerAgent agent,
        string target,
        CancellationToken ct)
    {
        var results = new List<string>();

        foreach (var script in agent.Scripts)
        {
            try
            {
                _logger.LogInformation("Executing script {ScriptName}...", script.ScriptName);

                // Create temporary file for script
                var tempFile = Path.Combine(Path.GetTempPath(), script.ScriptName);
                await File.WriteAllTextAsync(tempFile, script.Code, ct);

                // Execute script based on type
                var result = await ExecuteScriptAsync(tempFile, script.Language, target, ct);
                results.Add($"[{script.ScriptName}] {result}");

                // Cleanup
                File.Delete(tempFile);
            }
            catch (Exception ex)
            {
                results.Add($"[{script.ScriptName}] ERROR: {ex.Message}");
            }
        }

        // AI analysis of combined results
        var analysis = await AnalyzeResultsWithAIAsync(results, target, ct);
        results.Add($"[AI_ANALYSIS] {analysis}");

        return results;
    }

    private async Task<string> ExecuteScriptAsync(
        string scriptPath,
        string language,
        string target,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = GetInterpreter(language),
            Arguments = $"\"{scriptPath}\" \"{target}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? Path.GetTempPath()
        };

        using var process = new Process { StartInfo = psi };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null) output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null) error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Timeout after 5 minutes
        var timeout = TimeSpan.FromMinutes(5);
        var completed = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds), ct);

        if (!completed)
        {
            try { process.Kill(); } catch (Exception ex) { _logger.LogDebug(ex, "Failed to kill timed-out process"); }
            return "TIMEOUT: Script execution exceeded 5 minutes";
        }

        var result = output.ToString();
        var errors = error.ToString();

        if (!string.IsNullOrWhiteSpace(errors))
        {
            result += $"\nERRORS: {errors}";
        }

        return string.IsNullOrWhiteSpace(result)
            ? "Script executed successfully (no output)"
            : result;
    }

    private string GetInterpreter(string language) => language.ToLower() switch
    {
        "python" => "python3",
        "bash" => "bash",
        "powershell" => "powershell",
        "ruby" => "ruby",
        _ => "sh"
    };

    private async Task<string> AnalyzeResultsWithAIAsync(
        List<string> results,
        string target,
        CancellationToken ct)
    {
        var prompt = $@"
Analyze these security test results for target: {target}

Results:
{string.Join("\n", results)}

Provide:
1. Summary of findings
2. Risk assessment (Critical/High/Medium/Low)
3. Specific vulnerabilities found
4. Recommended fixes
5. Overall security posture

Keep response under 500 words.";

        return await _aiService.GenerateCompletionAsync(prompt, cancellationToken: ct);
    }

    private HackerAgentDto MapToDto(HackerAgent agent) => new()
    {
        Id = agent.Id,
        OperationId = agent.OperationId,
        WorkspaceId = agent.WorkspaceId,
        Scripts = agent.Scripts.Select(s => new SecurityScriptDto
        {
            Id = s.Id,
            ScriptName = s.ScriptName,
            Language = s.Language,
            Purpose = s.Purpose,
            CreatedAt = s.CreatedAt
        }).ToList(),
        Tools = agent.Tools.Select(t => new GitHubSecurityToolDto
        {
            Id = t.Id,
            RepoName = t.RepoName,
            Owner = t.Owner,
            Repo = t.Repo,
            Description = t.Description,
            FetchedAt = t.FetchedAt
        }).ToList(),
        TestResults = agent.TestResults,
        Status = agent.Status,
        CreatedAt = agent.CreatedAt,
        CompletedAt = agent.CompletedAt
    };
}
*/
