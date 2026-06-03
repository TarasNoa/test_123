using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Best-effort helpers for extracting JSON out of LLM replies. Free OpenRouter
/// models often wrap JSON in prose or triple backticks; these helpers are
/// tolerant of that.
/// </summary>
internal static partial class LlmJsonHelpers
{
    private static readonly Regex ThinkingBlockRegex = ThinkingBlockPattern();

    public static JsonDocument? ExtractJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var trimmed = StripCodeFences(raw.Trim());
        trimmed = StripThinkingBlocks(trimmed);

        if (TryParse(trimmed, out var doc)) return doc;

        var objectDoc = TryExtractBalancedJson(trimmed, '{', '}');
        if (objectDoc is not null) return objectDoc;

        var arrayDoc = TryExtractBalancedJson(trimmed, '[', ']');
        if (arrayDoc is not null) return arrayDoc;

        return null;
    }

    private static string StripCodeFences(string trimmed)
    {
        if (!trimmed.StartsWith("```", StringComparison.Ordinal)) return trimmed;

        var firstNewLine = trimmed.IndexOf('\n');
        if (firstNewLine > 0) trimmed = trimmed[(firstNewLine + 1)..];
        var fence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        if (fence > 0) trimmed = trimmed[..fence];
        return trimmed.Trim();
    }

    private static string StripThinkingBlocks(string input) =>
        ThinkingBlockRegex.Replace(input, string.Empty).Trim();

    private static JsonDocument? TryExtractBalancedJson(string trimmed, char open, char close)
    {
        var start = trimmed.IndexOf(open);
        if (start < 0) return null;

        var depth = 0;
        var inString = false;
        var escape = false;
        for (var i = start; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString) continue;

            if (c == open) depth++;
            else if (c == close)
            {
                depth--;
                if (depth == 0)
                {
                    var slice = trimmed[start..(i + 1)];
                    if (TryParse(slice, out var doc)) return doc;
                    var repaired = TryRepairTruncatedJson(slice);
                    return repaired is not null && TryParse(repaired, out doc) ? doc : null;
                }
            }
        }

        var tail = trimmed[start..];
        var repairedTail = TryRepairTruncatedJson(tail);
        return repairedTail is not null && TryParse(repairedTail, out var repairedDoc) ? repairedDoc : null;
    }

    private static string? TryRepairTruncatedJson(string input)
    {
        var stack = new Stack<char>();
        bool inString = false;
        bool escape = false;
        int lastSafeEnd = -1;
        Stack<char>? safeStack = null;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (escape) { escape = false; continue; }
            if (c == '\\' && inString) { escape = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;

            if (c == '{' || c == '[') stack.Push(c);
            else if (c == '}' || c == ']')
            {
                if (stack.Count > 0) stack.Pop();
            }
            else if (c == ',' && stack.Count > 0)
            {
                lastSafeEnd = i;
                safeStack = CloneStack(stack);
            }
        }

        int end;
        Stack<char> closingStack;

        if (inString)
        {
            if (lastSafeEnd < 0 || safeStack is null) return null;
            end = lastSafeEnd;
            closingStack = safeStack;
        }
        else if (stack.Count == 0)
        {
            return input;
        }
        else
        {
            if (lastSafeEnd > 0 && safeStack is not null)
            {
                end = lastSafeEnd;
                closingStack = safeStack;
            }
            else
            {
                end = input.Length;
                closingStack = stack;
            }
        }

        var sb = new System.Text.StringBuilder(end + closingStack.Count);
        sb.Append(input, 0, end);
        foreach (var open in closingStack)
        {
            sb.Append(open == '{' ? '}' : ']');
        }
        return sb.ToString();
    }

    private static Stack<char> CloneStack(Stack<char> source)
    {
        var reversed = new Stack<char>(source);
        return new Stack<char>(reversed);
    }

    public static string GetString(JsonElement element, string property, string fallback = "")
    {
        if (element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString() ?? fallback;
        return fallback;
    }

    public static int GetInt(JsonElement element, string property, int fallback)
    {
        if (element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.Number
            && prop.TryGetInt32(out var v))
            return v;
        return fallback;
    }

    public static List<string> GetStringArray(JsonElement element, string property)
    {
        var list = new List<string>();
        if (element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in prop.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) list.Add(s!);
                }
            }
        }
        return list;
    }

    [ThreadStatic]
    private static string? _lastParseError;

    public static string? LastParseError => _lastParseError;

    private static bool TryParse(string input, out JsonDocument? doc)
    {
        try
        {
            doc = JsonDocument.Parse(input);
            _lastParseError = null;
            return true;
        }
        catch (JsonException ex)
        {
            doc = null;
            _lastParseError = $"{ex.GetType().Name}@{ex.LineNumber}:{ex.BytePositionInLine}: {ex.Message}";
            Trace.WriteLine($"[LlmJsonHelpers] parse failed: {_lastParseError}");
            return false;
        }
        catch (Exception ex)
        {
            doc = null;
            _lastParseError = $"{ex.GetType().Name}: {ex.Message}";
            Trace.WriteLine($"[LlmJsonHelpers] parse error: {_lastParseError}");
            return false;
        }
    }

    [GeneratedRegex("<thinking>\\s*[\\s\\S]*?</thinking>", RegexOptions.IgnoreCase)]
    private static partial Regex ThinkingBlockPattern();
}
