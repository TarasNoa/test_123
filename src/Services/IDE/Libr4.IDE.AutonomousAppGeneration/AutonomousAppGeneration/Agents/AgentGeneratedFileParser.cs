using System.Text.Json;
using System.Text.RegularExpressions;
using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Parses agent LLM output into <see cref="GeneratedFile"/> entries (shared by handler + orchestrator).
/// </summary>
public static class AgentGeneratedFileParser
{
    private static readonly Regex FilesJsonAnchor = new(
        @"\{\s*""files""\s*:\s*\[",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex MarkdownFence = new(
        @"```(?:json)?\s*([\s\S]*?)```",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FileMarkerRegex = new(
        @"(?://|#)\s*File:\s*(.+?)\r?\n(.*?)(?=(?://|#)\s*File:|$)",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static bool HasParseableFiles(string? content) => TryParse(content).Count > 0;

    public static List<DomainGeneratedFile> TryParse(string? content)
    {
        var byPath = new Dictionary<string, DomainGeneratedFile>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(content))
            return new List<DomainGeneratedFile>();

        var normalized = StripMarkdownFences(content);

        foreach (var json in ExtractAllFilesJsonObjects(normalized))
            MergeParsedFiles(ParseFilesJson(json), byPath);

        if (byPath.Count == 0)
        {
            foreach (Match match in FileMarkerRegex.Matches(normalized))
            {
                var path = SanitizePath(match.Groups[1].Value);
                var fileContent = match.Groups[2].Value.Trim();
                if (path.Length > 0 && fileContent.Length > 0)
                    MergeOne(byPath, path, fileContent);
            }
        }

        return byPath.Values.ToList();
    }

    private static string StripMarkdownFences(string content)
    {
        var sb = new System.Text.StringBuilder(content);
        foreach (Match m in MarkdownFence.Matches(content))
        {
            if (!string.IsNullOrWhiteSpace(m.Groups[1].Value))
                sb.Append('\n').Append(m.Groups[1].Value);
        }

        return sb.ToString();
    }

    private static IEnumerable<string> ExtractAllFilesJsonObjects(string content)
    {
        var matches = FilesJsonAnchor.Matches(content);
        foreach (Match match in matches)
        {
            var start = content.IndexOf('{', match.Index);
            if (start < 0)
                continue;

            var json = ExtractBalancedJsonObject(content, start);
            if (json is not null)
                yield return json;
        }
    }

    private static void MergeParsedFiles(IEnumerable<DomainGeneratedFile> parsed, Dictionary<string, DomainGeneratedFile> byPath)
    {
        foreach (var file in parsed)
            MergeOne(byPath, file.RelativePath, file.Content, file.Language);
    }

    private static void MergeOne(
        Dictionary<string, DomainGeneratedFile> byPath,
        string rawPath,
        string content,
        string? language = null)
    {
        var path = SanitizePath(rawPath);
        if (path.Length == 0 || string.IsNullOrWhiteSpace(content))
            return;

        var file = new DomainGeneratedFile(path, language ?? InferLanguage(path), content);
        if (byPath.TryGetValue(path, out var existing))
        {
            if (content.Length > (existing.Content?.Length ?? 0))
                byPath[path] = file;
        }
        else
        {
            byPath[path] = file;
        }
    }

    private static List<DomainGeneratedFile> ParseFilesJson(string json)
    {
        var files = new List<DomainGeneratedFile>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("files", out var filesArray)
                || filesArray.ValueKind != JsonValueKind.Array)
                return files;

            foreach (var element in filesArray.EnumerateArray())
            {
                var path = ReadPath(element);
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                if (!element.TryGetProperty("content", out var contentEl))
                    continue;

                var fileContent = contentEl.ValueKind == JsonValueKind.String
                    ? contentEl.GetString()
                    : contentEl.GetRawText();

                if (fileContent is null)
                    continue;

                files.Add(new DomainGeneratedFile(SanitizePath(path), InferLanguage(path), fileContent));
            }
        }
        catch
        {
            // caller tries other strategies
        }

        return files;
    }

    private static string? ReadPath(JsonElement element)
    {
        if (element.TryGetProperty("relativePath", out var rel))
            return rel.GetString();
        if (element.TryGetProperty("path", out var path))
            return path.GetString();
        if (element.TryGetProperty("file", out var file))
            return file.GetString();
        return null;
    }

    private static string SanitizePath(string? raw) =>
        Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackArtifactCompleteness
            .SanitizeRelativePath(raw);

    private static string? ExtractBalancedJsonObject(string content, int start)
    {
        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = start; i < content.Length; i++)
        {
            var c = content[i];
            if (inString)
            {
                if (escape)
                    escape = false;
                else if (c == '\\')
                    escape = true;
                else if (c == '"')
                    inString = false;
                continue;
            }

            if (c == '"')
            {
                inString = true;
                continue;
            }

            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return content[start..(i + 1)];
            }
        }

        return null;
    }

    private static string InferLanguage(string path) =>
        Path.GetExtension(path).TrimStart('.').ToLowerInvariant() switch
        {
            "cs" => "csharp",
            "ts" or "tsx" => "typescript",
            "js" or "jsx" => "javascript",
            "py" => "python",
            "go" => "go",
            "rs" => "rust",
            "java" => "java",
            "php" => "php",
            "rb" => "ruby",
            "kt" => "kotlin",
            "scala" => "scala",
            "swift" => "swift",
            "dart" => "dart",
            "html" => "html",
            "css" => "css",
            "scss" or "sass" => "scss",
            "sql" => "sql",
            "yaml" or "yml" => "yaml",
            "json" => "json",
            "xml" => "xml",
            "md" => "markdown",
            "sh" or "bash" => "shell",
            "ps1" => "powershell",
            _ => "plaintext"
        };
}
