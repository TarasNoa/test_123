using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Scans JS/TS imports and ensures package.json lists required runtime dependencies (no LLM).
/// </summary>
public static class DependencySyncEngine
{
    private static readonly Regex ImportFrom = new(
        @"(?:import\s+(?:[\w*{}\s,]+\s+from\s+)?|require\s*\(\s*)[""']([^""']+)[""']",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly IReadOnlyDictionary<string, string> ImportToPackage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["jsonwebtoken"] = "^9.0.2",
        ["dotenv"] = "^16.4.5",
        ["express"] = "^4.21.0",
        ["cors"] = "^2.8.5",
        ["helmet"] = "^7.1.0",
        ["pino"] = "^9.4.0",
        ["pino-http"] = "^10.3.0",
        ["axios"] = "^1.7.7",
        ["zod"] = "^3.23.8",
        ["bcrypt"] = "^5.1.1",
        ["bcryptjs"] = "^2.4.3",
        ["mongoose"] = "^8.7.0",
        ["pg"] = "^8.13.0",
        ["mysql2"] = "^3.11.3",
        ["uuid"] = "^10.0.0",
        ["body-parser"] = "^1.20.3",
        ["cookie-parser"] = "^1.4.6",
        ["morgan"] = "^1.10.0",
        ["winston"] = "^3.15.0",
        ["react"] = "^18.3.1",
        ["react-dom"] = "^18.3.1",
        ["react-router-dom"] = "^6.27.0",
        ["next"] = "^14.2.0",
    };

    public static int SyncPackageJsonDependencies(IList<GeneratedFile> files)
    {
        var packageFiles = files
            .Where(f => f.RelativePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (packageFiles.Count == 0)
            return 0;

        var sources = files
            .Where(f => f.RelativePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                        || f.RelativePath.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
                        || f.RelativePath.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase)
                        || f.RelativePath.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase)
                        || f.RelativePath.EndsWith(".mjs", StringComparison.OrdinalIgnoreCase)
                        || f.RelativePath.EndsWith(".cjs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var required = CollectRequiredPackages(sources);
        if (required.Count == 0)
            return 0;

        var changed = 0;
        foreach (var pkgFile in packageFiles)
        {
            var ownerDir = GetDirectory(pkgFile.RelativePath);
            var ownedSources = sources
                .Where(s => IsUnderDirectory(s.RelativePath, ownerDir))
                .ToList();
            var scopedRequired = ownedSources.Count > 0
                ? CollectRequiredPackages(ownedSources)
                : required;

            var idx = files.ToList().FindIndex(f =>
                f.RelativePath.Equals(pkgFile.RelativePath, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                continue;

            if (TryMergeDependencies(files[idx].Content ?? "{}", scopedRequired, out var merged))
            {
                files[idx] = new GeneratedFile(files[idx].RelativePath, files[idx].Language, merged);
                changed++;
            }
        }

        return changed;
    }

    private static readonly Regex CannotFindPackage = new(
        @"(?:Cannot find module|Error:\s*Cannot find module)\s+['""]([^'""./][^'""]*)['""]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Adds npm packages referenced in build stderr to the nearest package.json.
    /// </summary>
    public static int SyncFromBuildLog(IList<GeneratedFile> files, string? buildLog)
    {
        if (string.IsNullOrWhiteSpace(buildLog))
            return 0;

        var packages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in CannotFindPackage.Matches(buildLog))
        {
            var spec = match.Groups[1].Value.Trim();
            if (spec.StartsWith('@'))
            {
                var scoped = string.Join('/', spec.Split('/').Take(2));
                if (ImportToPackage.ContainsKey(scoped) || ImportToPackage.ContainsKey(spec.Split('/')[0].TrimStart('@')))
                    packages.Add(scoped);
            }
            else
            {
                var pkg = spec.Split('/')[0];
                if (ImportToPackage.ContainsKey(pkg))
                    packages.Add(pkg);
            }
        }

        if (packages.Count == 0)
            return 0;

        var changed = 0;
        foreach (var pkgFile in files.Where(f =>
                     f.RelativePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            var idx = files.IndexOf(pkgFile);
            if (TryMergeDependencies(files[idx].Content ?? "{}", packages, out var merged))
            {
                files[idx] = new GeneratedFile(pkgFile.RelativePath, pkgFile.Language, merged);
                changed++;
            }
        }

        return changed;
    }

    private static HashSet<string> CollectRequiredPackages(IEnumerable<GeneratedFile> sources)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in sources)
        {
            var content = file.Content ?? string.Empty;
            foreach (Match m in ImportFrom.Matches(content))
            {
                var spec = m.Groups[1].Value.Trim();
                if (string.IsNullOrWhiteSpace(spec)
                    || spec.StartsWith(".", StringComparison.Ordinal)
                    || spec.StartsWith("/", StringComparison.Ordinal)
                    || spec.StartsWith("node:", StringComparison.OrdinalIgnoreCase))
                    continue;

                var pkg = spec.Split('/')[0];
                if (ImportToPackage.ContainsKey(pkg))
                    set.Add(pkg);
            }

            foreach (var known in ImportToPackage.Keys)
            {
                if (content.Contains($"from \"{known}\"", StringComparison.OrdinalIgnoreCase)
                    || content.Contains($"from '{known}'", StringComparison.OrdinalIgnoreCase)
                    || content.Contains($"require(\"{known}\")", StringComparison.OrdinalIgnoreCase)
                    || content.Contains($"require('{known}')", StringComparison.OrdinalIgnoreCase))
                    set.Add(known);
            }
        }

        return set;
    }

    private static bool TryMergeDependencies(string json, IReadOnlyCollection<string> packages, out string result)
    {
        result = json;
        try
        {
            var node = JsonNode.Parse(json) as JsonObject;
            if (node is null)
                return false;

            var deps = node["dependencies"] as JsonObject ?? new JsonObject();
            var devDeps = node["devDependencies"] as JsonObject;
            var changed = false;
            foreach (var pkg in packages)
            {
                if (deps.ContainsKey(pkg) || (devDeps?.ContainsKey(pkg) ?? false))
                    continue;

                deps[pkg] = ImportToPackage.TryGetValue(pkg, out var ver) ? ver : "^1.0.0";
                changed = true;
            }

            if (!changed)
                return false;

            node["dependencies"] = deps;
            result = node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetDirectory(string path)
    {
        var normalized = path.Replace('\\', '/');
        var last = normalized.LastIndexOf('/');
        return last <= 0 ? string.Empty : normalized[..last];
    }

    private static bool IsUnderDirectory(string filePath, string dir)
    {
        if (string.IsNullOrEmpty(dir))
            return !filePath.Contains('/', StringComparison.Ordinal);

        var fileDir = GetDirectory(filePath);
        return fileDir.Equals(dir, StringComparison.OrdinalIgnoreCase)
               || fileDir.StartsWith(dir + "/", StringComparison.OrdinalIgnoreCase);
    }
}
