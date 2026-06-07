using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Applies Claude Code-style search/replace edits to in-memory generated files.
/// </summary>
public static class SurgicalPatchEngine
{
    public sealed record SurgicalEdit(string RelativePath, string Search, string Replace);

    public sealed record NewFile(string RelativePath, string Content, string? Language = null);

    public sealed record ApplyResult(
        IReadOnlyList<GeneratedFile> Patches,
        int AppliedEdits,
        int SkippedEdits,
        IReadOnlyList<string> Warnings);

    public static ApplyResult Apply(
        IReadOnlyList<GeneratedFile> currentFiles,
        IReadOnlyList<SurgicalEdit> edits,
        IReadOnlyList<NewFile>? newFiles = null,
        IReadOnlyDictionary<string, string>? baseContents = null)
    {
        var working = currentFiles.ToDictionary(f => f.RelativePath, f => f, StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();
        var applied = 0;
        var skipped = 0;

        foreach (var edit in edits)
        {
            var path = FixerPatchScopePolicy.NormalizePatchRelativePath(edit.RelativePath);
            if (!working.TryGetValue(path, out var file))
            {
                warnings.Add($"edit_skipped_missing_file:{path}");
                skipped++;
                continue;
            }

            var content = file.Content ?? string.Empty;
            if (string.IsNullOrEmpty(edit.Search))
            {
                warnings.Add($"edit_skipped_empty_search:{path}");
                skipped++;
                continue;
            }

            var count = CountOccurrences(content, edit.Search);
            if (count == 0)
            {
                warnings.Add($"edit_skipped_search_not_found:{path}");
                skipped++;
                continue;
            }

            if (count > 1)
            {
                warnings.Add($"edit_applied_first_of_many:{path}:matches={count}");
            }

            var updated = ReplaceFirst(content, edit.Search, edit.Replace ?? string.Empty);
            if (string.Equals(content, updated, StringComparison.Ordinal))
            {
                skipped++;
                continue;
            }

            working[path] = new GeneratedFile(path, file.Language, updated);
            applied++;
        }

        foreach (var created in newFiles ?? Array.Empty<NewFile>())
        {
            var path = FixerPatchScopePolicy.NormalizePatchRelativePath(created.RelativePath);
            if (string.IsNullOrWhiteSpace(created.Content))
            {
                warnings.Add($"new_file_skipped_empty:{path}");
                continue;
            }

            if (working.ContainsKey(path))
            {
                var existing = working[path];
                if (!string.Equals(existing.Content, created.Content, StringComparison.Ordinal))
                {
                    working[path] = new GeneratedFile(path, created.Language ?? existing.Language, created.Content);
                    applied++;
                }

                continue;
            }

            working[path] = new GeneratedFile(
                path,
                created.Language ?? InferLanguage(path),
                created.Content);
            applied++;
        }

        var patches = working.Values
            .Where(candidate =>
            {
                var before = currentFiles.FirstOrDefault(f =>
                    f.RelativePath.Equals(candidate.RelativePath, StringComparison.OrdinalIgnoreCase));
                return before is null
                       || !string.Equals(before.Content, candidate.Content, StringComparison.Ordinal);
            })
            .ToList();

        return new ApplyResult(patches, applied, skipped, warnings);
    }

    private static int CountOccurrences(string content, string search)
    {
        if (string.IsNullOrEmpty(search))
            return 0;

        var count = 0;
        var idx = 0;
        while ((idx = content.IndexOf(search, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += search.Length;
        }

        return count;
    }

    private static string ReplaceFirst(string content, string search, string replace)
    {
        var idx = content.IndexOf(search, StringComparison.Ordinal);
        return idx < 0 ? content : content[..idx] + replace + content[(idx + search.Length)..];
    }

    private static bool TryThreeWayMerge(
        string baseContent,
        string currentContent,
        string search,
        string replace,
        out string merged)
    {
        merged = currentContent;
        if (!baseContent.Contains(search, StringComparison.Ordinal))
            return false;

        var baseUpdated = ReplaceFirst(baseContent, search, replace);
        if (string.Equals(baseUpdated, currentContent, StringComparison.Ordinal))
        {
            merged = baseUpdated;
            return true;
        }

        if (CountOccurrences(currentContent, search) == 1)
        {
            merged = ReplaceFirst(currentContent, search, replace);
            return true;
        }

        return false;
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
            ".py" => "python",
            _ => "text"
        };
    }
}
