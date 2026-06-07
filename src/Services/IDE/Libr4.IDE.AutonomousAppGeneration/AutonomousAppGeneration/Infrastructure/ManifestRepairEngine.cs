using System.Text.RegularExpressions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic manifest repair (no LLM): pom.xml, package.json, requirements.txt, .csproj basics.
/// </summary>
public static class ManifestRepairEngine
{
    private static readonly (string Wrong, string Right)[] MavenTagNormalizations =
    {
        ("<groupid>", "<groupId>"),
        ("</groupid>", "</groupId>"),
        ("<artifactid>", "<artifactId>"),
        ("</artifactid>", "</artifactId>"),
        ("<modelversion>", "<modelVersion>"),
        ("</modelversion>", "</modelVersion>"),
    };

    public static int RepairAll(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog = null)
    {
        var changed = 0;
        changed += UniversalManifestFixes.FixPackageJsonTemplateBraces(files);
        changed += RepairMavenPoms(files);
        changed += RepairDuplicatePomBuildSections(files);
        changed += DependencySyncEngine.SyncPackageJsonDependencies(files);
        changed += PythonDependencySyncEngine.SyncRequirements(files);
        changed += UniversalManifestFixes.FixRequirementsDuplicates(files, new List<string>(), autoFix: true);
        changed += RepairPyprojectTomlBasics(files);
        changed += NativeManifestSyncEngines.SyncGoMod(files);
        changed += NativeManifestSyncEngines.SyncCargoToml(files);
        changed += NativeManifestSyncEngines.SyncGemfile(files);
        changed += NativeManifestSyncEngines.RepairComposerJsonBraces(files);

        if (StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack)
            changed += JavaStructuralCompileRemediation.ApplyStructuralFixes(files, plan, buildLog);

        changed += CsprojPackageReconciler.ReconcilePackages(files) > 0 ? 1 : 0;
        return changed;
    }

    public static int RepairForQualityGate(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        IReadOnlyList<string> gateReasons) =>
        QualityGateStructuralRepair.Repair(files, plan, gateReasons);

    public static int RepairMavenPoms(IList<GeneratedFile> files)
    {
        var changed = 0;
        for (var i = 0; i < files.Count; i++)
        {
            if (!files[i].RelativePath.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = files[i].Content ?? string.Empty;
            var updated = content;
            foreach (var (wrong, right) in MavenTagNormalizations)
                updated = Regex.Replace(updated, Regex.Escape(wrong), right, RegexOptions.IgnoreCase);

            updated = FixMalformedDependencyBlocks(updated);
            if (string.Equals(updated, content, StringComparison.Ordinal))
                continue;

            files[i] = new GeneratedFile(files[i].RelativePath, files[i].Language, updated);
            changed++;
        }

        return changed;
    }

    public static int RepairDuplicatePomBuildSections(IList<GeneratedFile> files)
    {
        var changed = 0;
        for (var i = 0; i < files.Count; i++)
        {
            if (!files[i].RelativePath.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = files[i].Content ?? string.Empty;
            if (Regex.Matches(content, "<build>", RegexOptions.IgnoreCase).Count <= 1)
                continue;

            var merged = Regex.Replace(
                content,
                "</build>\\s*<build>",
                string.Empty,
                RegexOptions.IgnoreCase);
            if (string.Equals(merged, content, StringComparison.Ordinal))
                continue;

            files[i] = new GeneratedFile(files[i].RelativePath, files[i].Language, merged);
            changed++;
        }

        return changed;
    }

    public static int RepairPyprojectTomlBasics(IList<GeneratedFile> files)
    {
        var changed = 0;
        for (var i = 0; i < files.Count; i++)
        {
            if (!files[i].RelativePath.EndsWith("pyproject.toml", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = files[i].Content ?? string.Empty;
            var updated = content;
            updated = Regex.Replace(updated, @"\[project\]\s*\r?\n\s*name\s*=", "[project]\nname =", RegexOptions.IgnoreCase);
            updated = Regex.Replace(updated, @"\{\{\s*", "{", RegexOptions.IgnoreCase);
            updated = Regex.Replace(updated, @"\s*\}\}", "}", RegexOptions.IgnoreCase);
            if (string.Equals(updated, content, StringComparison.Ordinal))
                continue;

            files[i] = new GeneratedFile(files[i].RelativePath, files[i].Language, updated);
            changed++;
        }

        return changed;
    }

    private static string FixMalformedDependencyBlocks(string pom)
    {
        var updated = pom;
        updated = Regex.Replace(
            updated,
            "<dependency>\\s*<groupId>([^<]+)</groupId>\\s*</dependency>",
            "<dependency><groupId>$1</groupId><artifactId>unspecified</artifactId><version>0.0.1-SNAPSHOT</version></dependency>",
            RegexOptions.IgnoreCase);
        return updated;
    }
}
