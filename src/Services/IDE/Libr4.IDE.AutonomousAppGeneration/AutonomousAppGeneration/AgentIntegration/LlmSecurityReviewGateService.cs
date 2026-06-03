using System.Text;
using System.Text.Json;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// LLM security reviewer (security-testing skill). Replaces brittle regex gates with
/// context-aware analysis on the same cloud model as code generation (e.g. DeepSeek via OpenRouter).
/// </summary>
public sealed class LlmSecurityReviewGateService : ISecurityReviewGateService
{
    private const string SecurityTestingSkillRelativePath = "Agents/Skills/security-testing/SKILL.md";

    private static readonly string OutputContract = """
        ====================== OUTPUT (STRICT) ======================
        Return ONLY valid JSON (no markdown fences, no prose):
        {
          "score": <integer 0-10>,
          "passed": <boolean>,
          "findings": [
            {
              "severity": "critical|high|medium|low|info",
              "path": "<repo-relative path or empty for global>",
              "category": "<short category>",
              "message": "<what is wrong>",
              "recommendation": "<actionable fix or null>"
            }
          ]
        }

        Scoring guide:
        - 10: no material security issues for generated app artifacts
        - 7-9: minor issues only (dev-only placeholders with clear env injection path)
        - 4-6: serious issues that should be fixed before production
        - 0-3: critical blockers (embedded private keys, live cloud credentials, empty production secrets)

        ====================== FALSE POSITIVE GUARD ======================
        Do NOT penalize these legitimate patterns:
        - Java line continuations such as `UsernamePasswordAuthenticationToken authentication =` (next line continues the statement)
        - Non-empty JWT/config secrets including base64 padding `=` at end of line (e.g. jwt.secret=VGhl...=)
        - Spring `@Value("${...secret...}")` property placeholders (secrets loaded at runtime)
        - Test-only credentials inside paths clearly under tests/ or *Test.java / application-test.yml

        DO flag:
        - Empty secret/password values intended for production
        - Hardcoded live API keys, private keys, or production passwords in source
        - Obvious insecure defaults (password123, changeme) in non-test code
        """;

    private readonly IAIService _ai;
    private readonly ILogger<LlmSecurityReviewGateService> _logger;
    private readonly SecurityReviewGateOptions _options;
    private readonly SecurityReviewGateService _deterministic;
    private readonly string _skillInstructions;

    public LlmSecurityReviewGateService(
        IAIService ai,
        IOptions<SecurityReviewGateOptions> options,
        ILogger<LlmSecurityReviewGateService> logger)
    {
        _ai = ai;
        _logger = logger;
        _options = options.Value;
        _deterministic = new SecurityReviewGateService(options);
        _skillInstructions = LoadSkillInstructions();
    }

    public async Task<SecurityReviewAuditEntry> EvaluateArtifactsAsync(
        string stage,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        CancellationToken ct = default)
    {
        if (IsDeterministicMode())
            return _deterministic.EvaluateArtifacts(stage, files, plan);

        if (files.Count == 0)
        {
            return new SecurityReviewAuditEntry(
                stage,
                10,
                true,
                Array.Empty<string>(),
                Array.Empty<string>(),
                DateTime.UtcNow);
        }

        var selected = SelectFilesForReview(files);
        var prompt = BuildReviewPrompt(stage, plan, files, selected);
        prompt = PromptPipelinePolicy.ApplyInputBudget("security_review", prompt);

        _logger.LogInformation(
            "Security review agent: stage={Stage}, files={Selected}/{Total}, promptChars={Chars}, model={Model}",
            stage,
            selected.Count,
            files.Count,
            prompt.Length,
            _options.Model ?? "(host default)");

        string raw;
        try
        {
            raw = await _ai.GenerateCompletionAsync(
                prompt,
                $"{_skillInstructions}\n\n{OutputContract}",
                _options.Model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security review LLM call failed for stage {Stage}", stage);
            throw new AutonomousGenerationFailedException(
                "security_review",
                $"Security review LLM call failed: {ex.Message}",
                ex);
        }

        if (!PromptPipelinePolicy.ValidateOutputContract("security_review", raw, out var contractReason))
        {
            throw new AutonomousGenerationFailedException(
                "security_review",
                $"Security review output failed contract validation: {contractReason}");
        }

        return ParseReviewResponse(stage, raw);
    }

    private bool IsDeterministicMode() =>
        string.Equals(_options.Mode, "deterministic", StringComparison.OrdinalIgnoreCase);

    private SecurityReviewAuditEntry ParseReviewResponse(string stage, string raw)
    {
        using var doc = LlmJsonHelpers.ExtractJson(raw);
        if (doc is null)
        {
            throw new AutonomousGenerationFailedException(
                "security_review",
                $"Security review response is not valid JSON. parse={LlmJsonHelpers.LastParseError ?? "unknown"}");
        }

        var root = doc.RootElement;
        var score = root.TryGetProperty("score", out var scoreEl) && scoreEl.ValueKind == JsonValueKind.Number
            ? Math.Clamp(scoreEl.GetInt32(), 0, 10)
            : (int?)null;

        var findings = ParseFindings(root);
        var reasons = findings
            .Select(f => $"{f.Severity}:{f.Category}:{f.Path}")
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var hints = findings
            .Select(f => f.Recommendation)
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Distinct()
            .Cast<string>()
            .ToList();

        if (score is null)
        {
            score = DeriveScoreFromFindings(findings);
        }

        var minScore = Math.Clamp(_options.MinScore, 0, 10);
        var hasCritical = findings.Any(f =>
            f.Severity.Equals("critical", StringComparison.OrdinalIgnoreCase));
        var explicitPassed = root.TryGetProperty("passed", out var passedEl)
                             && passedEl.ValueKind == JsonValueKind.True;
        var passed = explicitPassed
            ? passedEl.GetBoolean() && score >= minScore && !hasCritical
            : score >= minScore && !hasCritical;

        if (!passed && reasons.Count == 0)
            reasons.Add("security_review:agent_rejected");

        _logger.LogInformation(
            "Security review agent completed: score={Score}, passed={Passed}, findings={Count}",
            score,
            passed,
            findings.Count);

        return new SecurityReviewAuditEntry(
            stage,
            score.Value,
            passed,
            reasons,
            hints,
            DateTime.UtcNow);
    }

    private static List<SecurityFinding> ParseFindings(JsonElement root)
    {
        var list = new List<SecurityFinding>();
        if (!root.TryGetProperty("findings", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var item in arr.EnumerateArray())
        {
            list.Add(new SecurityFinding(
                LlmJsonHelpers.GetString(item, "severity", "medium"),
                LlmJsonHelpers.GetString(item, "path", string.Empty),
                LlmJsonHelpers.GetString(item, "category", "general"),
                LlmJsonHelpers.GetString(item, "message", string.Empty),
                item.TryGetProperty("recommendation", out var rec) && rec.ValueKind == JsonValueKind.String
                    ? rec.GetString()
                    : null));
        }

        return list;
    }

    private static int DeriveScoreFromFindings(IReadOnlyList<SecurityFinding> findings)
    {
        var score = 10;
        foreach (var f in findings)
        {
            score -= f.Severity.ToLowerInvariant() switch
            {
                "critical" => 3,
                "high" => 2,
                "medium" => 1,
                _ => 0
            };
        }

        return Math.Clamp(score, 0, 10);
    }

    private string BuildReviewPrompt(
        string stage,
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> allFiles,
        IReadOnlyList<GeneratedFile> selected)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## SECURITY REVIEW REQUEST");
        sb.AppendLine($"Stage: {stage}");
        sb.AppendLine($"Application: {plan.ApplicationName}");
        sb.AppendLine($"Description: {Trim(plan.ApplicationDescription, 1200)}");
        sb.AppendLine(
            $"Tech stack: {string.Join(", ", plan.TechStack.Languages)} / {string.Join(", ", plan.TechStack.Frameworks)}");
        sb.AppendLine($"Artifacts total: {allFiles.Count}; included in this review: {selected.Count}");
        sb.AppendLine();
        sb.AppendLine("## FILE MANIFEST (all paths)");
        foreach (var path in allFiles.Select(f => f.RelativePath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            sb.AppendLine($"- {path}");

        sb.AppendLine();
        sb.AppendLine("## FILE CONTENTS FOR REVIEW");
        var budget = Math.Max(8_000, _options.MaxTotalPromptChars);
        foreach (var file in selected)
        {
            var content = file.Content ?? string.Empty;
            if (content.Length > _options.MaxCharsPerFile)
                content = content[.._options.MaxCharsPerFile] + "\n/* … truncated for review budget … */";

            var block = $"### {file.RelativePath}\n```\n{content}\n```\n";
            if (sb.Length + block.Length > budget)
            {
                sb.AppendLine("/* Additional files omitted — review manifest paths and prioritize critical findings. */");
                break;
            }

            sb.Append(block);
        }

        return sb.ToString();
    }

    public static IReadOnlyList<GeneratedFile> SelectFilesForReview(
        IReadOnlyList<GeneratedFile> files,
        int maxFiles)
    {
        if (files.Count <= maxFiles)
            return files.ToList();

        return files
            .Select(f => (File: f, Score: ScoreSecurityRelevance(f.RelativePath)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.File.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Take(maxFiles)
            .Select(x => x.File)
            .ToList();
    }

    private IReadOnlyList<GeneratedFile> SelectFilesForReview(IReadOnlyList<GeneratedFile> files) =>
        SelectFilesForReview(files, Math.Max(1, _options.MaxFilesToReview));

    public static int ScoreSecurityRelevance(string path)
    {
        var p = path.Replace('\\', '/').ToLowerInvariant();
        var score = 0;

        if (p.Contains("/security/", StringComparison.Ordinal) ||
            p.Contains("/auth/", StringComparison.Ordinal) ||
            p.Contains("jwt", StringComparison.Ordinal))
            score += 30;

        if (p.EndsWith(".properties", StringComparison.Ordinal) ||
            p.EndsWith(".env", StringComparison.Ordinal) ||
            p.Contains("docker-compose", StringComparison.Ordinal))
            score += 25;

        if (p.Contains("application.yml", StringComparison.Ordinal) ||
            p.Contains("application.yaml", StringComparison.Ordinal) ||
            p.Contains("appsettings", StringComparison.Ordinal))
            score += 22;

        if (p.EndsWith(".sh", StringComparison.Ordinal) ||
            p.EndsWith("dockerfile", StringComparison.Ordinal))
            score += 12;

        if (p.Contains("/controller/", StringComparison.Ordinal) ||
            p.Contains("securityconfig", StringComparison.Ordinal))
            score += 10;

        if (p.Contains("/test/", StringComparison.Ordinal) ||
            p.Contains("/tests/", StringComparison.Ordinal) ||
            p.EndsWith("test.java", StringComparison.Ordinal))
            score -= 15;

        return score;
    }

    private static string LoadSkillInstructions()
    {
        var skillPath = Path.Combine(AppContext.BaseDirectory, SecurityTestingSkillRelativePath);
        if (File.Exists(skillPath))
            return File.ReadAllText(skillPath);

        return """
            You are a security testing expert for generated application code.
            Focus on OWASP risks, secret handling, auth boundaries, and unsafe defaults.
            Be precise: distinguish dev placeholders from production blockers.
            """;
    }

    private static string Trim(string? value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max
            ? value ?? string.Empty
            : value[..max] + "…";

    private sealed record SecurityFinding(
        string Severity,
        string Path,
        string Category,
        string Message,
        string? Recommendation);
}
