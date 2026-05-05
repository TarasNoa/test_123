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
            "fixing" => 72_000,
            "error_analysis" => 56_000,
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
            "generation" or "fixing" => ValidateFilesEnvelope(root, out reason),
            "error_analysis" => ValidateErrorEnvelope(root, out reason),
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

    private static bool ValidateFilesEnvelope(JsonElement root, out string reason)
    {
        reason = string.Empty;
        if (!root.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Array)
        {
            reason = "missing_files_array";
            return false;
        }

        foreach (var item in files.EnumerateArray())
        {
            if (!item.TryGetProperty("relativePath", out var p) || p.ValueKind != JsonValueKind.String)
            {
                reason = "missing_file_relativePath";
                return false;
            }

            if (!item.TryGetProperty("content", out var c) || c.ValueKind != JsonValueKind.String)
            {
                reason = "missing_file_content";
                return false;
            }
        }

        return true;
    }

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
}

