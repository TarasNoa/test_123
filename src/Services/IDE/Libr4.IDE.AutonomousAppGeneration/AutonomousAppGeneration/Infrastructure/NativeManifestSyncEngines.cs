using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic sync for go.mod, Cargo.toml, Gemfile, composer.json (no LLM).
/// </summary>
public static class NativeManifestSyncEngines
{
    private static readonly IReadOnlyDictionary<string, string> GoModuleToVersion = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["github.com/gin-gonic/gin"] = "v1.10.0",
        ["github.com/go-chi/chi/v5"] = "v5.1.0",
        ["github.com/gorilla/mux"] = "v1.8.1",
        ["github.com/lib/pq"] = "v1.10.9",
        ["gorm.io/gorm"] = "v1.25.12",
        ["github.com/golang-jwt/jwt/v5"] = "v5.2.1",
    };

    private static readonly IReadOnlyDictionary<string, string> RustCrateToVersion = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["axum"] = "0.7.7",
        ["tokio"] = "1.41.0",
        ["serde"] = "1.0.210",
        ["serde_json"] = "1.0.128",
        ["sqlx"] = "0.8.2",
        ["tracing"] = "0.1.40",
        ["actix_web"] = "4.9.0",
    };

    private static readonly IReadOnlyDictionary<string, string> RubyGemToVersion = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["rails"] = "~> 7.2.0",
        ["pg"] = "~> 1.5",
        ["puma"] = "~> 6.4",
        ["rack"] = "~> 3.1",
        ["sinatra"] = "~> 4.0",
    };

    public static int SyncGoMod(IList<GeneratedFile> files)
    {
        var modFiles = files.Where(f => f.RelativePath.EndsWith("go.mod", StringComparison.OrdinalIgnoreCase)).ToList();
        if (modFiles.Count == 0)
            return 0;

        var sources = files.Where(f => f.RelativePath.EndsWith(".go", StringComparison.OrdinalIgnoreCase)).ToList();
        var modules = CollectReferencedKeys(sources, GoModuleToVersion.Keys);
        if (modules.Count == 0)
            return 0;

        return ApplyGoMerge(modFiles, files, modules);
    }

    public static int SyncCargoToml(IList<GeneratedFile> files)
    {
        var cargoFiles = files.Where(f => f.RelativePath.EndsWith("Cargo.toml", StringComparison.OrdinalIgnoreCase)).ToList();
        if (cargoFiles.Count == 0)
            return 0;

        var sources = files.Where(f => f.RelativePath.EndsWith(".rs", StringComparison.OrdinalIgnoreCase)).ToList();
        var crates = CollectRustCrates(sources);
        if (crates.Count == 0)
            return 0;

        return ApplyCargoMerge(cargoFiles, files, crates);
    }

    public static int SyncGemfile(IList<GeneratedFile> files)
    {
        var gemfiles = files.Where(f => f.RelativePath.EndsWith("Gemfile", StringComparison.OrdinalIgnoreCase)).ToList();
        if (gemfiles.Count == 0)
            return 0;

        var sources = files.Where(f => f.RelativePath.EndsWith(".rb", StringComparison.OrdinalIgnoreCase)).ToList();
        var gems = CollectReferencedKeys(sources, RubyGemToVersion.Keys);
        if (gems.Count == 0)
            return 0;

        return ApplyGemMerge(gemfiles, files, gems);
    }

    public static int RepairComposerJsonBraces(IList<GeneratedFile> files)
    {
        var changed = 0;
        for (var i = 0; i < files.Count; i++)
        {
            if (!files[i].RelativePath.EndsWith("composer.json", StringComparison.OrdinalIgnoreCase))
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

    private static HashSet<string> CollectReferencedKeys(
        IEnumerable<GeneratedFile> sources,
        IEnumerable<string> keys)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in sources)
        {
            var content = file.Content ?? string.Empty;
            foreach (var key in keys)
            {
                if (content.Contains(key, StringComparison.OrdinalIgnoreCase))
                    set.Add(key);
            }
        }

        return set;
    }

    private static HashSet<string> CollectRustCrates(IEnumerable<GeneratedFile> sources)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in sources)
        {
            var content = file.Content ?? string.Empty;
            foreach (var crate in RustCrateToVersion.Keys)
            {
                if (content.Contains($"use {crate}", StringComparison.OrdinalIgnoreCase)
                    || content.Contains($"{crate}::", StringComparison.OrdinalIgnoreCase))
                    set.Add(crate);
            }
        }

        return set;
    }

    private static int ApplyGoMerge(List<GeneratedFile> manifestFiles, IList<GeneratedFile> files, HashSet<string> keys)
    {
        var changed = 0;
        foreach (var manifest in manifestFiles)
        {
            var idx = IndexOfPath(files, manifest.RelativePath);
            if (idx < 0)
                continue;
            if (TryMergeGoMod(files[idx].Content ?? string.Empty, keys, out var merged))
            {
                files[idx] = new GeneratedFile(files[idx].RelativePath, files[idx].Language, merged);
                changed++;
            }
        }

        return changed;
    }

    private static int ApplyCargoMerge(List<GeneratedFile> manifestFiles, IList<GeneratedFile> files, HashSet<string> keys)
    {
        var changed = 0;
        foreach (var manifest in manifestFiles)
        {
            var idx = IndexOfPath(files, manifest.RelativePath);
            if (idx < 0)
                continue;
            if (TryMergeCargoToml(files[idx].Content ?? string.Empty, keys, out var merged))
            {
                files[idx] = new GeneratedFile(files[idx].RelativePath, files[idx].Language, merged);
                changed++;
            }
        }

        return changed;
    }

    private static int ApplyGemMerge(List<GeneratedFile> manifestFiles, IList<GeneratedFile> files, HashSet<string> keys)
    {
        var changed = 0;
        foreach (var manifest in manifestFiles)
        {
            var idx = IndexOfPath(files, manifest.RelativePath);
            if (idx < 0)
                continue;
            if (TryMergeGemfile(files[idx].Content ?? string.Empty, keys, out var merged))
            {
                files[idx] = new GeneratedFile(files[idx].RelativePath, files[idx].Language, merged);
                changed++;
            }
        }

        return changed;
    }

    private static bool TryMergeGoMod(string content, HashSet<string> modules, out string result)
    {
        result = content;
        var updated = content;
        var changed = false;
        foreach (var mod in modules)
        {
            if (!GoModuleToVersion.TryGetValue(mod, out var ver))
                continue;
            if (updated.Contains(mod, StringComparison.OrdinalIgnoreCase))
                continue;
            updated += $"\nrequire {mod} {ver}";
            changed = true;
        }

        if (!changed)
            return false;
        result = updated.TrimEnd() + "\n";
        return true;
    }

    private static bool TryMergeCargoToml(string content, HashSet<string> crates, out string result)
    {
        result = content;
        var updated = content;
        if (!updated.Contains("[dependencies]", StringComparison.OrdinalIgnoreCase))
            updated += "\n[dependencies]\n";

        var changed = false;
        foreach (var crate in crates)
        {
            if (!RustCrateToVersion.TryGetValue(crate, out var ver))
                continue;
            if (Regex.IsMatch(updated, $"^\\s*{Regex.Escape(crate)}\\s*=", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                continue;
            updated += $"{crate} = \"{ver}\"\n";
            changed = true;
        }

        if (!changed)
            return false;
        result = updated;
        return true;
    }

    private static bool TryMergeGemfile(string content, HashSet<string> gems, out string result)
    {
        result = content;
        var updated = content;
        var changed = false;
        foreach (var gem in gems)
        {
            if (!RubyGemToVersion.TryGetValue(gem, out var ver))
                continue;
            if (updated.Contains($"gem '{gem}'", StringComparison.OrdinalIgnoreCase)
                || updated.Contains($"gem \"{gem}\"", StringComparison.OrdinalIgnoreCase))
                continue;
            updated += $"gem '{gem}', '{ver}'\n";
            changed = true;
        }

        if (!changed)
            return false;
        result = updated;
        return true;
    }

    private static int IndexOfPath(IList<GeneratedFile> files, string path) =>
        files.ToList().FindIndex(f => f.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase));
}
