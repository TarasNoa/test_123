using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.SecurityTesting;

/// <summary>
/// Security testing service: uses dotnet CLI for dependency vulnerability scan
/// and regex-based secrets scanning on source files.
/// </summary>
public class SecurityTestingService : ISecurityTestingService
{
    private readonly ILogger<SecurityTestingService> _logger;

    private static readonly Regex SecretPattern = new(
        @"(password|secret|api[_-]?key|token|private[_-]?key|access[_-]?key)\s*[=:]\s*[""']?[A-Za-z0-9+/=_\-]{8,}[""']?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public SecurityTestingService(ILogger<SecurityTestingService> logger)
    {
        _logger = logger;
    }

    public async Task<SecurityTestResult> RunTestsAsync(
        string targetPath,
        SecurityTestOptions? options = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Running security tests on {TargetPath}", targetPath);
        options ??= new SecurityTestOptions();

        var vulnerabilities = new List<Vulnerability>();
        var logs = new List<string>();

        if (options.RunDependencyScan)
        {
            var depReport = await ScanDependenciesAsync(targetPath, ct);
            vulnerabilities.AddRange(depReport.Vulnerabilities);
            logs.Add($"Dependency scan: {depReport.TotalDependencies} deps, {depReport.VulnerableDependencies} vulnerable");
        }

        if (options.RunSecretsScan && Directory.Exists(targetPath))
        {
            var secrets = ScanForSecrets(targetPath, options.ExcludePatterns, _logger);
            vulnerabilities.AddRange(secrets);
            logs.Add($"Secrets scan: {secrets.Count} potential secrets found");
        }

        return new SecurityTestResult
        {
            Success = true,
            Vulnerabilities = vulnerabilities.ToArray(),
            Logs = logs.ToArray(),
            CompletedAt = DateTime.UtcNow
        };
    }

    public async Task<VulnerabilityReport> ScanDependenciesAsync(
        string projectPath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Scanning dependencies in {ProjectPath}", projectPath);

        var csprojFiles = Directory.Exists(projectPath)
            ? Directory.EnumerateFiles(projectPath, "*.csproj", SearchOption.AllDirectories).ToList()
            : File.Exists(projectPath) ? new List<string> { projectPath } : new List<string>();

        if (csprojFiles.Count == 0)
            return new VulnerabilityReport { TotalDependencies = 0, VulnerableDependencies = 0, Vulnerabilities = Array.Empty<Vulnerability>() };

        var vulnerabilities = new List<Vulnerability>();
        var totalDeps = 0;

        foreach (var csproj in csprojFiles)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var (output, exitCode) = await RunDotnetAsync(
                    $"list \"{csproj}\" package --vulnerable --include-transitive",
                    ct);

                if (exitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    var parsed = ParseVulnerablePackages(output, csproj);
                    vulnerabilities.AddRange(parsed.vulns);
                    totalDeps += parsed.total;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dependency scan failed for {CsProj}", csproj);
            }
        }

        return new VulnerabilityReport
        {
            TotalDependencies = totalDeps,
            VulnerableDependencies = vulnerabilities.Select(v => v.Title).Distinct().Count(),
            Vulnerabilities = vulnerabilities.ToArray()
        };
    }

    private static async Task<(string Output, int ExitCode)> RunDotnetAsync(
        string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet process");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(60));

        var stdout = await process.StandardOutput.ReadToEndAsync(cts.Token);
        await process.WaitForExitAsync(cts.Token);
        return (stdout, process.ExitCode);
    }

    private static (List<Vulnerability> vulns, int total) ParseVulnerablePackages(
        string output, string csproj)
    {
        var vulns = new List<Vulnerability>();
        var total = 0;
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(">", StringComparison.Ordinal))
                total++;

            if (trimmed.Contains("Critical", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("High", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("Moderate", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("Low", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var severity = parts.FirstOrDefault(p =>
                        p.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
                        p.Equals("High", StringComparison.OrdinalIgnoreCase) ||
                        p.Equals("Moderate", StringComparison.OrdinalIgnoreCase) ||
                        p.Equals("Low", StringComparison.OrdinalIgnoreCase)) ?? "Unknown";

                    vulns.Add(new Vulnerability
                    {
                        Id = $"DEP-{Guid.NewGuid():N}"[..12],
                        Title = parts[1],
                        Severity = severity,
                        FilePath = csproj,
                        Description = trimmed
                    });
                }
            }
        }
        return (vulns, total);
    }

    private static List<Vulnerability> ScanForSecrets(
        string rootPath, string[]? excludePatterns, ILogger? logger = null)
    {
        var results = new List<Vulnerability>();
        var extensions = new[] { "*.cs", "*.ts", "*.js", "*.json", "*.yaml", "*.yml", "*.env" };

        foreach (var ext in extensions)
        {
            foreach (var file in Directory.EnumerateFiles(rootPath, ext, SearchOption.AllDirectories))
            {
                if (excludePatterns != null && excludePatterns.Any(p =>
                    file.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (file.Contains(".git", StringComparison.Ordinal) ||
                    file.Contains("node_modules", StringComparison.Ordinal))
                    continue;

                try
                {
                    var lines = File.ReadAllLines(file);
                    for (var i = 0; i < lines.Length; i++)
                    {
                        if (SecretPattern.IsMatch(lines[i]))
                        {
                            results.Add(new Vulnerability
                            {
                                Id = $"SECRET-{i + 1}",
                                Title = "Potential hardcoded secret",
                                Severity = "High",
                                FilePath = file,
                                LineNumber = i + 1,
                                Description = $"Line {i + 1}: possible credential pattern detected"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "Failed to read file {File}", file);
                }
            }
        }
        return results;
    }
}
