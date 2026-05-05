using System.Diagnostics;
using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Best-effort helpers for extracting JSON out of LLM replies. Free OpenRouter
/// models often wrap JSON in prose or triple backticks; these helpers are
/// tolerant of that.
/// </summary>
internal static class LlmJsonHelpers
{
    public static JsonDocument? ExtractJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // Strip ```json ... ``` or ``` ... ``` fences.
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("```"))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            if (firstNewLine > 0) trimmed = trimmed[(firstNewLine + 1)..];
            var fence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (fence > 0) trimmed = trimmed[..fence];
        }

        // Try direct parse first.
        if (TryParse(trimmed, out var doc)) return doc;

        // Otherwise grab the first balanced JSON block.
        var start = trimmed.IndexOfAny(new[] { '{', '[' });
        if (start < 0) return null;
        var slice = trimmed[start..];
        if (TryParse(slice, out doc)) return doc;

        // Last resort: the stream may have been truncated mid-generation.
        // Attempt to repair by closing any dangling braces/brackets.
        var repaired = TryRepairTruncatedJson(slice);
        return repaired is not null && TryParse(repaired, out doc) ? doc : null;
    }

    private static string? TryRepairTruncatedJson(string input)
    {
        // Walk the string tracking string-literal state, escapes, and open
        // braces/brackets. Keep a snapshot of the bracket stack at the most
        // recent safe cut point (a comma outside any string at depth > 0).
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
                // Safe cut point: element completed at current depth. Snapshot the stack.
                lastSafeEnd = i;
                safeStack = CloneStack(stack);
            }
        }

        int end;
        Stack<char> closingStack;

        if (inString)
        {
            // Truncated inside a string value. Must roll back to last safe comma.
            if (lastSafeEnd < 0 || safeStack is null) return null;
            end = lastSafeEnd; // drop the trailing comma
            closingStack = safeStack;
        }
        else if (stack.Count == 0)
        {
            // Nothing to close; input was balanced already.
            return input;
        }
        else
        {
            // Not inside a string. Trim any trailing incomplete token (e.g. `"key":`) by
            // rolling back to the last safe comma if one exists.
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
        foreach (var open in closingStack) // iteration yields top-of-stack first
        {
            sb.Append(open == '{' ? '}' : ']');
        }
        return sb.ToString();
    }

    private static Stack<char> CloneStack(Stack<char> source)
    {
        // Preserve top-of-stack ordering so iteration of the clone still yields top first.
        var reversed = new Stack<char>(source); // reversed: bottom first on top
        return new Stack<char>(reversed);       // double-reverse restores original order
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

    /// <summary>P0-7: last JSON parse failure captured per logical flow (best-effort thread-local).</summary>
    [ThreadStatic]
    private static string? _lastParseError;

    /// <summary>Returns the most recent parse error captured on the current async context (or null).</summary>
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
            // Surface to anyone listening on .NET trace listeners without coupling to ILogger.
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
}
