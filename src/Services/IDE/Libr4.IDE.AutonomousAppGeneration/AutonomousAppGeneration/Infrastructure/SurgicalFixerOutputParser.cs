using System.Text.Json;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Parses Claude Code-style surgical repair JSON (search/replace edits + optional new files).
/// </summary>
public static class SurgicalFixerOutputParser
{
    public sealed record ParsedSurgicalResponse(
        IReadOnlyList<SurgicalPatchEngine.SurgicalEdit> Edits,
        IReadOnlyList<SurgicalPatchEngine.NewFile> NewFiles);

    public static ParsedSurgicalResponse Parse(string raw, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Empty();

        using var doc = LlmJsonHelpers.ExtractJson(raw);
        if (doc is null)
        {
            logger?.LogDebug("Surgical parser: JSON extract failed ({Reason})", LlmJsonHelpers.LastParseError);
            return Empty();
        }

        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return Empty();

        var edits = ParseEdits(root, logger);
        var newFiles = ParseNewFiles(root, logger);
        return new ParsedSurgicalResponse(edits, newFiles);
    }

    private static List<SurgicalPatchEngine.SurgicalEdit> ParseEdits(JsonElement root, ILogger? logger)
    {
        var list = new List<SurgicalPatchEngine.SurgicalEdit>();
        if (!root.TryGetProperty("edits", out var edits) || edits.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var item in edits.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var path = ResolvePath(item);
            var search = ResolveString(item, "search", "old", "oldText", "old_text", "find");
            var replace = ResolveString(item, "replace", "new", "newText", "new_text", "with", "content");
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(search))
                continue;

            list.Add(new SurgicalPatchEngine.SurgicalEdit(path, search, replace ?? string.Empty));
        }

        if (list.Count == 0)
            logger?.LogDebug("Surgical parser: edits array had no usable entries.");

        return list;
    }

    private static List<SurgicalPatchEngine.NewFile> ParseNewFiles(JsonElement root, ILogger? logger)
    {
        var list = new List<SurgicalPatchEngine.NewFile>();
        if (!root.TryGetProperty("newFiles", out var files) || files.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var item in files.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var path = ResolvePath(item);
            var content = ResolveString(item, "content", "body", "source", "code", "text");
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(content))
                continue;

            list.Add(new SurgicalPatchEngine.NewFile(path, content));
        }

        if (list.Count == 0)
            logger?.LogDebug("Surgical parser: newFiles array had no usable entries.");

        return list;
    }

    private static string? ResolvePath(JsonElement item)
    {
        foreach (var key in new[] { "relativePath", "relative_path", "path", "filePath", "file_path", "file" })
        {
            if (item.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                var value = prop.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return null;
    }

    private static string? ResolveString(JsonElement item, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!item.TryGetProperty(key, out var prop))
                continue;
            if (prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
        }

        return null;
    }

    private static ParsedSurgicalResponse Empty() =>
        new(Array.Empty<SurgicalPatchEngine.SurgicalEdit>(), Array.Empty<SurgicalPatchEngine.NewFile>());
}
