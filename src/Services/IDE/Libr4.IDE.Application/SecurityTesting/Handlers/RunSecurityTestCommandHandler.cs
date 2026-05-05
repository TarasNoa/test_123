/*
using MediatR;
using Libr4.IDE.Application.SecurityTesting.Commands;
using Libr4.IDE.Application.SecurityTesting.DTOs;
using Libr4.AI.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.SecurityTesting.Handlers;

/// <summary>
/// Handler for RunSecurityTestCommand - AI-powered security testing without F# dependency
/// </summary>
public class RunSecurityTestCommandHandler : IRequestHandler<RunSecurityTestCommand, SecurityTestResultDto>
{
    private readonly IAIService _aiService;
    private readonly ILogger<RunSecurityTestCommandHandler> _logger;

    public RunSecurityTestCommandHandler(
        IAIService aiService,
        ILogger<RunSecurityTestCommandHandler> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<SecurityTestResultDto> Handle(RunSecurityTestCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Starting security test {TestType} for {Target}", request.TestType, request.Target);

        var vulnerabilities = new List<SecurityVulnerability>();

        // Pattern-based security scan
        vulnerabilities.AddRange(await RunPatternScanAsync(request.Target, ct));

        // AI-powered deep scan for complex vulnerabilities
        if (request.TestType.Contains("Deep") || request.TestType.Contains("AI"))
        {
            var aiVulns = await RunAIDeepScanAsync(request.Target, ct);
            vulnerabilities.AddRange(aiVulns);
        }

        // Calculate scores
        var criticalCount = vulnerabilities.Count(v => v.Severity == "Critical");
        var highCount = vulnerabilities.Count(v => v.Severity == "High");
        var mediumCount = vulnerabilities.Count(v => v.Severity == "Medium");
        var lowCount = vulnerabilities.Count(v => v.Severity == "Low");
        var total = vulnerabilities.Count;

        // Security score: 100 - weighted deductions
        var securityScore = Math.Max(0, 100 - (criticalCount * 20 + highCount * 10 + mediumCount * 5 + lowCount * 1));

        _logger.LogInformation(
            "Security test completed: {Total} vulnerabilities found (Critical: {Critical}, High: {High}, Medium: {Medium}, Low: {Low})",
            total, criticalCount, highCount, mediumCount, lowCount);

        return new SecurityTestResultDto
        {
            TotalVulnerabilities = total,
            CriticalCount = criticalCount,
            HighCount = highCount,
            MediumCount = mediumCount,
            LowCount = lowCount,
            SecurityScore = securityScore
        };
    }

    private async Task<List<SecurityVulnerability>> RunPatternScanAsync(string target, CancellationToken ct)
    {
        var vulns = new List<SecurityVulnerability>();

        try
        {
            // Scan files in target directory
            if (Directory.Exists(target))
            {
                var files = Directory.GetFiles(target, "*.cs", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var content = await File.ReadAllTextAsync(file, ct);
                    var fileVulns = ScanFileForVulnerabilities(file, content);
                    vulns.AddRange(fileVulns);
                }
            }
            else if (File.Exists(target))
            {
                var content = await File.ReadAllTextAsync(target, ct);
                var fileVulns = ScanFileForVulnerabilities(target, content);
                vulns.AddRange(fileVulns);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pattern scan failed for {Target}", target);
        }

        return vulns;
    }

    private List<SecurityVulnerability> ScanFileForVulnerabilities(string filePath, string content)
    {
        var vulns = new List<SecurityVulnerability>();
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;

            // SQL Injection patterns
            if (Regex.IsMatch(line, @"(SqlCommand|ExecuteSqlCommand|FromSqlRaw|FromSqlInterpolated).*\+"))
            {
                vulns.Add(new SecurityVulnerability
                {
                    Type = "SQL Injection",
                    Severity = "Critical",
                    FilePath = filePath,
                    LineNumber = lineNumber,
                    Description = "Potential SQL injection vulnerability",
                    Remediation = "Use parameterized queries or ORM methods"
                });
            }

            // Hardcoded secrets
            if (Regex.IsMatch(line, @"(password|secret|key|token)\s*=\s*[""'][^""']+[""']", RegexOptions.IgnoreCase))
            {
                if (!line.Contains("Environment"))
                {
                    vulns.Add(new SecurityVulnerability
                    {
                        Type = "Hardcoded Secret",
                        Severity = "Critical",
                        FilePath = filePath,
                        LineNumber = lineNumber,
                        Description = "Hardcoded credential or secret detected",
                        Remediation = "Use environment variables or secret management"
                    });
                }
            }

            // Insecure deserialization
            if (line.Contains("BinaryFormatter") || line.Contains("Newtonsoft.Json") && line.Contains("TypeNameHandling"))
            {
                vulns.Add(new SecurityVulnerability
                {
                    Type = "Insecure Deserialization",
                    Severity = "High",
                    FilePath = filePath,
                    LineNumber = lineNumber,
                    Description = "Potentially insecure deserialization",
                    Remediation = "Use safe deserialization methods"
                });
            }

            // Weak crypto
            if (Regex.IsMatch(line, @"(MD5|SHA1|DES|RC4)", RegexOptions.IgnoreCase))
            {
                vulns.Add(new SecurityVulnerability
                {
                    Type = "Weak Cryptography",
                    Severity = "High",
                    FilePath = filePath,
                    LineNumber = lineNumber,
                    Description = "Weak cryptographic algorithm detected",
                    Remediation = "Use AES-256-GCM or ChaCha20-Poly1305"
                });
            }

            // XSS vulnerability
            if (Regex.IsMatch(line, @"Response\.Write|innerHTML\s*=|Html\.Raw", RegexOptions.IgnoreCase))
            {
                vulns.Add(new SecurityVulnerability
                {
                    Type = "XSS",
                    Severity = "High",
                    FilePath = filePath,
                    LineNumber = lineNumber,
                    Description = "Potential XSS vulnerability",
                    Remediation = "Use HTML encoding for user input"
                });
            }

            // Information disclosure
            if (line.Contains("stackTrace") || line.Contains("Exception") && line.Contains("ToString"))
            {
                vulns.Add(new SecurityVulnerability
                {
                    Type = "Information Disclosure",
                    Severity = "Medium",
                    FilePath = filePath,
                    LineNumber = lineNumber,
                    Description = "Potential information disclosure",
                    Remediation = "Log details internally, show generic message to users"
                });
            }
        }

        return vulns;
    }

    private async Task<List<SecurityVulnerability>> RunAIDeepScanAsync(string target, CancellationToken ct)
    {
        var vulns = new List<SecurityVulnerability>();

        try
        {
            var prompt = $"""
                Perform deep security analysis on: {target}
                
                Look for:
                1. Business logic vulnerabilities
                2. Race conditions
                3. Access control issues
                4. API security problems
                5. Authentication/Authorization flaws
                
                Return findings as: Severity|Type|Description|Fix
                """;

            var response = await _aiService.GenerateCompletionAsync(prompt, cancellationToken: ct);

            // Parse AI response for vulnerabilities
            var lines = response.Split('\n');
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length >= 3)
                {
                    vulns.Add(new SecurityVulnerability
                    {
                        Severity = parts[0].Trim(),
                        Type = parts[1].Trim(),
                        Description = parts[2].Trim(),
                        Remediation = parts.Length > 3 ? parts[3].Trim() : "Review and fix",
                        FilePath = target,
                        LineNumber = 0
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI deep scan failed for {Target}", target);
        }

        return vulns;
    }

    private class SecurityVulnerability
    {
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Remediation { get; set; } = string.Empty;
    }
}
*/
