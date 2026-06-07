using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Computer;

public static class ComputerFlowRequestParser
{
    public static ComputerFlowRequest Parse(string task, ToolContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(task);

        if (TryParseJson(task, out var jsonRequest))
            return jsonRequest;

        var flow = DetectFlowFromText(task);
        var url = ExtractUrl(task);
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(url))
            parameters["url"] = url!;

        return new ComputerFlowRequest(flow, url, parameters, task);
    }

    private static bool TryParseJson(string task, out ComputerFlowRequest request)
    {
        request = null!;
        var trimmed = task.Trim();
        if (!trimmed.StartsWith('{'))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;
            var flow = root.TryGetProperty("flow", out var flowEl) && flowEl.ValueKind == JsonValueKind.String
                ? flowEl.GetString()
                : null;
            var url = root.TryGetProperty("url", out var urlEl) && urlEl.ValueKind == JsonValueKind.String
                ? urlEl.GetString()
                : null;

            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(url))
                parameters["url"] = url!;

            if (root.TryGetProperty("parameters", out var paramsEl) && paramsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in paramsEl.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                        parameters[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }

            foreach (var key in new[] { "username", "password", "field_selector", "field_value", "submit_selector",
                         "username_selector", "password_selector", "success_selector", "form_selector" })
            {
                if (root.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String)
                    parameters[key] = el.GetString() ?? string.Empty;
            }

            request = new ComputerFlowRequest(flow, url, parameters, task);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? DetectFlowFromText(string task)
    {
        var lower = task.ToLowerInvariant();
        if (lower.Contains("login", StringComparison.Ordinal) || lower.Contains("sign in", StringComparison.Ordinal))
            return ComputerFlowNames.LoginFlow;
        if (lower.Contains("form fill", StringComparison.Ordinal) || lower.Contains("fill form", StringComparison.Ordinal))
            return ComputerFlowNames.FormFill;
        if (lower.Contains("visual", StringComparison.Ordinal) || lower.Contains("design check", StringComparison.Ordinal))
            return ComputerFlowNames.VisualDesignCheck;
        return null;
    }

    private static string? ExtractUrl(string task)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            task,
            @"https?://[^\s""']+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Value.TrimEnd('.', ',', ';') : null;
    }

}
