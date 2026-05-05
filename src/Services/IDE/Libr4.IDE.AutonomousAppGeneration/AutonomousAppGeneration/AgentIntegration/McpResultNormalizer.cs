using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

internal static class McpResultNormalizer
{
    public static string Normalize(JsonElement result)
    {
        if (result.ValueKind == JsonValueKind.Undefined || result.ValueKind == JsonValueKind.Null)
            return string.Empty;

        if (TryExtractText(result, out var text) && !string.IsNullOrWhiteSpace(text))
            return Truncate(text!);

        var raw = result.GetRawText();
        return Truncate(raw);
    }

    private static bool TryExtractText(JsonElement element, out string? text)
    {
        text = null;
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                text = element.GetString();
                return true;

            case JsonValueKind.Object:
                foreach (var key in new[] { "text", "summary", "message", "result", "content", "data" })
                {
                    if (!element.TryGetProperty(key, out var prop))
                        continue;
                    if (TryExtractText(prop, out text) && !string.IsNullOrWhiteSpace(text))
                        return true;
                }
                return false;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (TryExtractText(item, out text) && !string.IsNullOrWhiteSpace(text))
                        return true;
                }
                return false;

            default:
                return false;
        }
    }

    private static string Truncate(string text)
    {
        const int max = 2048;
        return text.Length <= max ? text : text[..max] + "…";
    }
}
