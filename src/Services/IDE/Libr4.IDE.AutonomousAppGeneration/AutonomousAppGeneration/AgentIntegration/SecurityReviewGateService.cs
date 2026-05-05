using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class SecurityReviewGateService : ISecurityReviewGateService
{
    private static readonly Regex AwsAccessKey = new(
        @"AKIA[0-9A-Z]{16}",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(150));

    private static readonly Regex PasswordLiteral = new(
        @"(password|passwd|pwd|secret)\s*=\s*[""'][^""'\r\n]{6,}[""']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    private static readonly Regex DangerousShell = new(
        @"\b(rm\s+-rf|mkfs\.|curl[^;\n]*\|\s*bash|wget[^;\n]*\|\s*sh)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    private static readonly Regex InsecureDefaultSecret = new(
        @"(dev-secret-change-me|password123|admin123|changeme|placeholder)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    private readonly SecurityReviewGateOptions _options;

    public SecurityReviewGateService(IOptions<SecurityReviewGateOptions> options)
    {
        _options = options.Value;
    }

    public SecurityReviewAuditEntry EvaluateArtifacts(
        string stage,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan)
    {
        _ = plan;
        var reasons = new List<string>();
        var hints = new List<string>();
        var score = 10;

        foreach (var file in files)
        {
            var path = file.RelativePath;
            var content = file.Content ?? string.Empty;

            // Skip security checks for test files - they are expected to have test data and credentials
            if (IsTestFile(path))
                continue;

            if (content.Contains("-----BEGIN PRIVATE KEY-----", StringComparison.Ordinal) ||
                content.Contains("-----BEGIN RSA PRIVATE KEY-----", StringComparison.Ordinal))
            {
                score -= 3;
                reasons.Add($"private_key_material:{path}");
                hints.Add($"Remove embedded key material from {path}; load secrets from environment or vault.");
            }

            if (AwsAccessKey.IsMatch(content))
            {
                score -= 3;
                reasons.Add($"aws_access_key_pattern:{path}");
                hints.Add("Replace static AWS access keys with IAM roles or secret injection.");
            }

            if (PasswordLiteral.IsMatch(content))
            {
                score -= 2;
                reasons.Add($"hardcoded_credential_literal:{path}");
                hints.Add("Avoid hardcoded passwords/secrets; use configuration providers with user-secrets in dev only.");
            }

            if (path.EndsWith(".sh", StringComparison.OrdinalIgnoreCase) &&
                DangerousShell.IsMatch(content))
            {
                score -= 2;
                reasons.Add($"dangerous_shell_construct:{path}");
                hints.Add("Remove destructive or curl|bash patterns from generated shell scripts.");
            }

            if (content.Contains("DisableHttpsMetadata", StringComparison.Ordinal) &&
                path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                score -= 1;
                reasons.Add($"insecure_https_metadata:{path}");
                hints.Add("Do not disable HTTPS metadata validation in production OpenID/JWT clients.");
            }

            // Check for insecure default secrets (P0.3)
            if (InsecureDefaultSecret.IsMatch(content))
            {
                score -= 3;
                reasons.Add($"insecure_default_secret:{path}");
                hints.Add("Replace insecure default secrets (dev-secret-change-me, secret, password123) with environment-driven configuration with proper fail-fast for empty values.");
            }

            // Check for empty secret values in env/config files (P0.3)
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Contains('=') && trimmedLine.EndsWith('=') ||
                    trimmedLine.Contains("=\"") || trimmedLine.Contains("='"))
                {
                    var keyPart = trimmedLine.Split('=')[0].Trim();
                    if (IsSecretKey(keyPart))
                    {
                        score -= 2;
                        reasons.Add($"empty_secret_value:{path}");
                        hints.Add("Secret values must not be empty; implement fail-fast startup when required secrets are missing.");
                        break;
                    }
                }
            }

            // Check for test tokens without auth flow (P0.3)
            if (content.Contains("test_token", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("demo_token", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("mock_token", StringComparison.OrdinalIgnoreCase))
            {
                if (!content.Contains("auth", StringComparison.OrdinalIgnoreCase))
                {
                    score -= 2;
                    reasons.Add($"test_token_without_auth:{path}");
                    hints.Add("Test tokens must be accompanied by proper authentication flow implementation.");
                }
            }
        }

        score = Math.Clamp(score, 0, 10);
        var passed = score >= Math.Clamp(_options.MinScore, 0, 10);
        return new SecurityReviewAuditEntry(
            stage,
            score,
            passed,
            reasons,
            hints,
            DateTime.UtcNow);
    }

    private static bool IsTestFile(string path)
    {
        var lowerPath = path.ToLowerInvariant();
        return lowerPath.Contains("/test/") ||
               lowerPath.Contains("/tests/") ||
               lowerPath.Contains("\\test\\") ||
               lowerPath.Contains("\\tests\\") ||
               lowerPath.EndsWith("_test.py") ||
               lowerPath.EndsWith("_test.js") ||
               lowerPath.EndsWith("_test.ts") ||
               lowerPath.EndsWith(".test.js") ||
               lowerPath.EndsWith(".test.ts") ||
               lowerPath.EndsWith(".spec.js") ||
               lowerPath.EndsWith(".spec.ts") ||
               lowerPath.Contains("conftest.py") ||
               lowerPath.Contains("test_services.py");
    }

    private static bool IsSecretKey(string key)
    {
        var lowerKey = key.ToLowerInvariant();
        return lowerKey.Contains("password") ||
               lowerKey.Contains("secret") ||
               lowerKey.Contains("api_key") ||
               lowerKey.Contains("jwt_secret") ||
               lowerKey.Contains("auth_token") ||
               lowerKey.Contains("secret_key") ||
               lowerKey.Contains("database_password");
    }
}
