using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;

/// <summary>Manifest fixes shared across stacks (package.json, requirements, docker-compose).</summary>
public static class UniversalManifestFixes
{
    public static int FixPackageJsonTemplateBraces(IList<GeneratedFile> files)
    {
        var changed = 0;
        for (var i = 0; i < files.Count; i++)
        {
            if (!files[i].RelativePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = files[i].Content ?? string.Empty;
            if (!content.Contains("{{", StringComparison.Ordinal))
                continue;

            files[i] = new GeneratedFile(
                files[i].RelativePath,
                files[i].Language,
                content.Replace("{{", "{", StringComparison.Ordinal).Replace("}}", "}", StringComparison.Ordinal));
            changed++;
        }

        return changed;
    }

    public static int FixRequirementsDuplicates(IList<GeneratedFile> files, List<string> warnings, bool autoFix)
    {
        var reqFiles = files
            .Where(f => f.RelativePath.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (reqFiles.Count <= 1)
            return 0;

        warnings.Add($"Multiple requirements.txt files: {string.Join(", ", reqFiles.Select(r => r.RelativePath))}");
        if (!autoFix)
            return 0;

        var keep = reqFiles
            .OrderBy(r => r.RelativePath.Equals("requirements.txt", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(r => r.RelativePath.Count(c => c == '/'))
            .First();
        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            if (reqFiles.Any(r => r.RelativePath.Equals(files[i].RelativePath, StringComparison.OrdinalIgnoreCase))
                && !files[i].RelativePath.Equals(keep.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                files.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }
}
