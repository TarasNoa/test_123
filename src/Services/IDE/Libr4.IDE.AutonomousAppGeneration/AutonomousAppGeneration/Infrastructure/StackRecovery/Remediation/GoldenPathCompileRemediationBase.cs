using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Remediation;

/// <summary>Shared deterministic fixes for Tier 1 golden-path compile remediation.</summary>
internal static class GoldenPathCompileRemediationBase
{
    public static int RemoveBrokenTestFiles(IList<GeneratedFile> files, params string[] testPathPrefixes)
    {
        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            var path = files[i].RelativePath.Replace('\\', '/');
            if (!IsTestPath(path))
                continue;

            if (testPathPrefixes.Length > 0
                && !testPathPrefixes.Any(p => path.Contains(p, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (LooksLikeBrokenGeneratedTest(files[i].Content))
            {
                files.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }

    public static int RemoveDuplicateByPathContains(
        IList<GeneratedFile> files,
        string pathFragment,
        string preferPath)
    {
        var matches = files
            .Where(f => f.RelativePath.Contains(pathFragment, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matches.Count <= 1)
            return 0;

        var keep = matches
            .OrderByDescending(m => m.RelativePath.Contains(preferPath, StringComparison.OrdinalIgnoreCase))
            .ThenBy(m => m.RelativePath.Count(c => c == '/'))
            .First();

        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            if (matches.Any(m => m.RelativePath.Equals(files[i].RelativePath, StringComparison.OrdinalIgnoreCase))
                && !files[i].RelativePath.Equals(keep.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                files.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }

    public static int DedupeConcatenatedSourceFiles(IList<GeneratedFile> files, string extension)
    {
        var changed = 0;
        foreach (var file in files.Where(f => f.RelativePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            var content = file.Content ?? string.Empty;
            var markerCount = CountDuplicateTypeDeclarations(content, extension);
            if (markerCount <= 1)
                continue;

            var split = SplitAtSecondDeclaration(content, extension);
            if (split is null || split == content)
                continue;

            file.Update(split);
            changed++;
        }

        return changed;
    }

    private static bool IsTestPath(string path) =>
        path.Contains("/test/", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("_test.go", StringComparison.OrdinalIgnoreCase)
        || path.Contains(".test.", StringComparison.OrdinalIgnoreCase)
        || path.Contains(".spec.", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeBrokenGeneratedTest(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return true;

        return content.Contains("TODO: fix test", StringComparison.OrdinalIgnoreCase)
               || content.Contains("cannot find symbol", StringComparison.OrdinalIgnoreCase)
               || content.Contains("import error", StringComparison.OrdinalIgnoreCase)
               || content.Contains("// broken", StringComparison.OrdinalIgnoreCase);
    }

    private static int CountDuplicateTypeDeclarations(string content, string extension) => extension switch
    {
        ".java" => content.Split("public class ", StringSplitOptions.None).Length - 1
                   + content.Split("public interface ", StringSplitOptions.None).Length - 1,
        ".cs" => content.Split("public class ", StringSplitOptions.None).Length - 1,
        ".py" => content.Split("class ", StringSplitOptions.None).Length - 1,
        ".go" => content.Split("type ", StringSplitOptions.None).Length - 1,
        ".rs" => content.Split("struct ", StringSplitOptions.None).Length - 1,
        ".php" => content.Split("class ", StringSplitOptions.None).Length - 1,
        _ => 1
    };

    private static string? SplitAtSecondDeclaration(string content, string extension)
    {
        var marker = extension switch
        {
            ".java" => "public class ",
            ".cs" => "public class ",
            ".py" => "class ",
            ".go" => "type ",
            ".rs" => "struct ",
            ".php" => "class ",
            _ => null
        };
        if (marker is null)
            return null;

        var first = content.IndexOf(marker, StringComparison.Ordinal);
        if (first < 0)
            return null;
        var second = content.IndexOf(marker, first + marker.Length, StringComparison.Ordinal);
        return second < 0 ? null : content[..second].TrimEnd();
    }
}
