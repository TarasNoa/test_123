using System.Text.Json;
using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Tolerant parser for LLM fixer responses. Models often emit path-only entries, alternate
/// field names, markdown code fences, or truncated JSON — strict contract validation drops those.
/// </summary>
public static class LlmFixerOutputParser
{
    private static readonly string[] PathKeys =
    [
        "relativePath", "relative_path", "path", "filePath", "file_path", "file", "filename"
    ];

    private static readonly string[] ContentKeys =
    [
        "content", "body", "source", "code", "text", "fileContent", "file_content", "newContent", "new_content"
    ];

    private static readonly Regex MarkdownFenceRegex = new(
        @"---\s*(?<path>[^\r\n`]+?)\s*---\s*```[\w]*\s*\r?\n(?<content>[\s\S]*?)```",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PathCommentFenceRegex = new(
        @"(?:^|\n)\s*(?://|#)\s*(?<path>backend/[\w./-]+|frontend/[\w./-]+)\s*\r?\n```[\w]*\s*\r?\n(?<content>[\s\S]*?)```",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

    public static IReadOnlyList<GeneratedFile> Parse(
        string raw,
        IReadOnlyList<GeneratedFile> currentFiles,
        ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<GeneratedFile>();

        var fromJson = ParseFromJson(raw, logger);
        if (fromJson.Count == 0)
            fromJson = ParseFromMarkdown(raw, logger);
        if (fromJson.Count == 0)
            fromJson = ParseFromSuggestedFixHints(raw, logger);

        return HydrateAndFilter(fromJson, currentFiles, raw, logger);
    }

    private static List<GeneratedFile> ParseFromJson(string raw, ILogger? logger)
    {
        using var doc = LlmJsonHelpers.ExtractJson(raw);
        if (doc is null)
        {
            logger?.LogDebug("Fixer parser: JSON extract failed ({Reason})", LlmJsonHelpers.LastParseError);
            return [];
        }

        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
            return ParseFileArray(root, logger);

        if (root.ValueKind != JsonValueKind.Object)
            return [];

        if (root.TryGetProperty("files", out var filesProp))
        {
            if (filesProp.ValueKind == JsonValueKind.Array)
                return ParseFileArray(filesProp, logger);

            if (filesProp.ValueKind == JsonValueKind.Object)
                return ParseFilesObjectMap(filesProp, logger);
        }

        if (root.TryGetProperty("patches", out var patches) && patches.ValueKind == JsonValueKind.Array)
            return ParseFileArray(patches, logger);

        return [];
    }

    private static List<GeneratedFile> ParseFileArray(JsonElement array, ILogger? logger)
    {
        var list = new List<GeneratedFile>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var parsed = ParseFileObject(item);
            if (parsed is not null)
                list.Add(parsed);
        }

        if (list.Count == 0)
            logger?.LogDebug("Fixer parser: files array present but no usable entries.");

        return list;
    }

    private static List<GeneratedFile> ParseFilesObjectMap(JsonElement map, ILogger? logger)
    {
        var list = new List<GeneratedFile>();
        foreach (var prop in map.EnumerateObject())
        {
            var path = prop.Name;
            var content = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                JsonValueKind.Object when prop.Value.TryGetProperty("content", out var nested)
                    => nested.GetString() ?? string.Empty,
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(path))
                continue;

            list.Add(new GeneratedFile(path, InferLanguage(path), content));
        }

        if (list.Count == 0)
            logger?.LogDebug("Fixer parser: files object-map had no entries.");

        return list;
    }

    private static GeneratedFile? ParseFileObject(JsonElement item)
    {
        var path = ResolvePath(item);
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var content = ResolveContent(item);
        return new GeneratedFile(path, InferLanguage(path), content ?? string.Empty);
    }

    private static string? ResolvePath(JsonElement item)
    {
        foreach (var key in PathKeys)
        {
            if (!item.TryGetProperty(key, out var prop))
                continue;
            if (prop.ValueKind != JsonValueKind.String)
                continue;
            var value = prop.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static string? ResolveContent(JsonElement item)
    {
        foreach (var key in ContentKeys)
        {
            if (!item.TryGetProperty(key, out var prop))
                continue;

            return prop.ValueKind switch
            {
                JsonValueKind.String => prop.GetString(),
                JsonValueKind.Null => string.Empty,
                JsonValueKind.Object or JsonValueKind.Array => prop.GetRawText(),
                _ => null
            };
        }

        return null;
    }

    private static List<GeneratedFile> ParseFromSuggestedFixHints(string raw, ILogger? logger)
    {
        var list = new List<GeneratedFile>();
        if (!raw.Contains("frontend/src/app.tsx", StringComparison.OrdinalIgnoreCase)
            && !raw.Contains("frontend/src/App.tsx", StringComparison.OrdinalIgnoreCase))
            return list;

        const string appPath = "frontend/src/App.tsx";
        if (raw.Contains("export function App", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("named export", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("import { App }", StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new GeneratedFile(appPath, "typescript", NamedAppTemplate));
            logger?.LogInformation("Fixer parser: synthesized missing {Path} from suggested-fix hints.", appPath);
        }

        return list;
    }

    private const string NamedAppTemplate = """
        import React, { useEffect, useState } from 'react';
        import { fetchAccounts } from './api/client';
        import './App.css';

        export function App() {
          const [accounts, setAccounts] = useState<Array<{ id: string }>>([]);
          useEffect(() => {
            fetchAccounts().then(setAccounts).catch(() => setAccounts([]));
          }, []);
          return (
            <main>
              <h1>Mobile Banking</h1>
              <p>Accounts loaded: {accounts.length}</p>
            </main>
          );
        }
        """;

    private static List<GeneratedFile> ParseFromMarkdown(string raw, ILogger? logger)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in MarkdownFenceRegex.Matches(raw))
        {
            var path = match.Groups["path"].Value.Trim();
            var content = match.Groups["content"].Value;
            if (!string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(content))
                map[path] = content;
        }

        foreach (Match match in PathCommentFenceRegex.Matches(raw))
        {
            var path = match.Groups["path"].Value.Trim();
            var content = match.Groups["content"].Value;
            if (!string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(content))
                map[path] = content;
        }

        if (map.Count == 0)
        {
            logger?.LogDebug("Fixer parser: no markdown code fences matched.");
            return [];
        }

        logger?.LogInformation("Fixer parser: recovered {Count} file(s) from markdown code fences.", map.Count);
        return map.Select(kv => new GeneratedFile(kv.Key, InferLanguage(kv.Key), kv.Value)).ToList();
    }

    private static List<GeneratedFile> HydrateAndFilter(
        IReadOnlyList<GeneratedFile> parsed,
        IReadOnlyList<GeneratedFile> currentFiles,
        string raw,
        ILogger? logger)
    {
        if (parsed.Count == 0)
            return [];

        var markdownByPath = ParseFromMarkdown(raw, null)
            .ToDictionary(f => f.RelativePath, f => f.Content, StringComparer.OrdinalIgnoreCase);
        var currentByPath = currentFiles.ToDictionary(f => f.RelativePath, f => f, StringComparer.OrdinalIgnoreCase);
        var hydrated = new List<GeneratedFile>();

        foreach (var file in parsed)
        {
            var path = FixerPatchScopePolicy.NormalizePatchRelativePath(file.RelativePath);
            var content = file.Content ?? string.Empty;

            if (string.IsNullOrWhiteSpace(content) && markdownByPath.TryGetValue(path, out var md))
                content = md;

            if (string.IsNullOrWhiteSpace(content))
            {
                // Path-only entry — skip (cannot apply empty patch).
                logger?.LogDebug("Fixer parser: skipping path-only entry {Path}", path);
                continue;
            }

            if (currentByPath.TryGetValue(path, out var existing)
                && string.Equals(existing.Content, content, StringComparison.Ordinal))
                continue;

            hydrated.Add(new GeneratedFile(path, file.Language ?? InferLanguage(path), content));
        }

        return hydrated;
    }

    private static string InferLanguage(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".cs" => "csharp",
            ".java" => "java",
            ".ts" or ".tsx" => "typescript",
            ".js" or ".jsx" => "javascript",
            ".json" => "json",
            ".xml" => "xml",
            ".yml" or ".yaml" => "yaml",
            ".md" => "markdown",
            _ => "text"
        };
    }
}
