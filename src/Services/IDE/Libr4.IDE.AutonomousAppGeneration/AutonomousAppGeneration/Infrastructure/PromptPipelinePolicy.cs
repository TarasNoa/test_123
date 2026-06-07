using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

public static class PromptPipelinePolicy
{
    public static string ApplyInputBudget(string stage, string prompt)
    {
        var maxChars = stage.ToLowerInvariant() switch
        {
            "planning" => 48_000,
            "generation" => 64_000,
            "fixing" => 96_000,
            "error_analysis" => 56_000,
            "security_review" => 120_000,
            _ => 48_000
        };

        if (string.IsNullOrEmpty(prompt) || prompt.Length <= maxChars)
            return prompt;

        var suffix = "\n\n[truncated_by_prompt_budget_policy=true]";
        var keep = Math.Max(0, maxChars - suffix.Length);
        return prompt[..keep] + suffix;
    }

    public static bool ValidateOutputContract(string stage, string rawOutput, out string reason)
    {
        reason = string.Empty;
        using var doc = LlmJsonHelpers.ExtractJson(rawOutput);
        if (doc is null)
        {
            reason = "json_extract_failed";
            return false;
        }

        var root = doc.RootElement;
        return stage.ToLowerInvariant() switch
        {
            "planning" => ValidatePlanning(root, out reason),
            "generation" => ValidateFilesEnvelope(root, out reason, lenient: false),
            "fixing" => ValidateFilesEnvelope(root, out reason, lenient: true),
            "error_analysis" => ValidateErrorEnvelope(root, out reason),
            "security_review" => ValidateSecurityReviewEnvelope(root, out reason),
            _ => true
        };
    }

    private static bool ValidatePlanning(JsonElement root, out string reason)
    {
        reason = string.Empty;
        if (!root.TryGetProperty("applicationName", out var app) || app.ValueKind != JsonValueKind.String)
        {
            reason = "missing_applicationName";
            return false;
        }

        if (!root.TryGetProperty("techStack", out var stack) || stack.ValueKind != JsonValueKind.Object)
        {
            reason = "missing_techStack";
            return false;
        }

        if (!stack.TryGetProperty("languages", out var langs) || langs.ValueKind != JsonValueKind.Array)
        {
            reason = "missing_techStack_languages";
            return false;
        }

        if (!root.TryGetProperty("phases", out var phases) || phases.ValueKind != JsonValueKind.Array)
        {
            reason = "missing_phases";
            return false;
        }

        return true;
    }

    private static bool ValidateFilesEnvelope(JsonElement root, out string reason, bool lenient = false)
    {
        reason = string.Empty;
        if (!root.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Array)
        {
            reason = "missing_files_array";
            return false;
        }

        var validEntries = 0;
        foreach (var item in files.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var hasPath = HasStringProperty(item, "relativePath")
                          || HasStringProperty(item, "path")
                          || HasStringProperty(item, "filePath")
                          || HasStringProperty(item, "file");
            if (!hasPath)
            {
                if (!lenient)
                {
                    reason = "missing_file_relativePath";
                    return false;
                }

                continue;
            }

            var hasContent = HasStringProperty(item, "content")
                             || HasStringProperty(item, "body")
                             || HasStringProperty(item, "code")
                             || HasStringProperty(item, "source");
            if (!hasContent)
            {
                if (!lenient)
                {
                    reason = "missing_file_content";
                    return false;
                }

                continue;
            }

            validEntries++;
        }

        if (lenient)
            return validEntries > 0 || files.GetArrayLength() == 0;

        return files.GetArrayLength() > 0;
    }

    private static bool HasStringProperty(JsonElement item, string name) =>
        item.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String;

    private static bool ValidateErrorEnvelope(JsonElement root, out string reason)
    {
        reason = string.Empty;
        if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Array)
        {
            reason = "missing_errors_array";
            return false;
        }

        return true;
    }

    private static bool ValidateSecurityReviewEnvelope(JsonElement root, out string reason)
    {
        reason = string.Empty;
        if (!root.TryGetProperty("score", out var score) || score.ValueKind != JsonValueKind.Number)
        {
            reason = "missing_score";
            return false;
        }

        if (!root.TryGetProperty("findings", out var findings) || findings.ValueKind != JsonValueKind.Array)
        {
            reason = "missing_findings_array";
            return false;
        }

        return true;
    }
}

