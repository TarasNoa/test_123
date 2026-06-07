using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic Python dependency sync: import scan, transitive deps, build-log reconciliation (no LLM).
/// </summary>
public static class PythonDependencySyncEngine
{
    private static readonly Regex ImportFrom = new(
        @"(?:^|\n)\s*from\s+([\w.]+)\s+import|(?:^|\n)\s*import\s+([\w.]+)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex ModuleNotFound = new(
        @"(?:ModuleNotFoundError:\s*)?No module named ['""]([\w.-]+)['""]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly (Regex Pattern, string Spec)[] TransitiveByUsage =
    [
        (new Regex(@"\bEmailStr\b", RegexOptions.Compiled), "email-validator>=2.0.0"),
        (new Regex(@"\bfrom\s+slowapi\b|\bimport\s+slowapi\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            "slowapi>=0.1.9"),
        (new Regex(@"\bpytest_asyncio\b|\bimport\s+pytest_asyncio\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            "pytest-asyncio>=0.23.0"),
        (new Regex(@"\bAsyncClient\b|\bASGITransport\b", RegexOptions.Compiled),
            "httpx>=0.27.0"),
    ];

    private static readonly IReadOnlyDictionary<string, string> ModuleToRequirement =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["fastapi"] = "fastapi>=0.115.0",
            ["uvicorn"] = "uvicorn[standard]>=0.30.0",
            ["sqlalchemy"] = "sqlalchemy>=2.0.35",
            ["pydantic"] = "pydantic>=2.9.0",
            ["httpx"] = "httpx>=0.27.0",
            ["pytest"] = "pytest>=8.3.0",
            ["django"] = "django>=5.1.0",
            ["flask"] = "flask>=3.0.0",
            ["requests"] = "requests>=2.32.0",
            ["dotenv"] = "python-dotenv>=1.0.1",
            ["jwt"] = "PyJWT>=2.9.0",
            ["alembic"] = "alembic>=1.13.0",
            ["redis"] = "redis>=5.1.0",
            ["celery"] = "celery>=5.4.0",
            ["numpy"] = "numpy>=2.1.0",
            ["pandas"] = "pandas>=2.2.0",
            ["yaml"] = "pyyaml>=6.0.2",
            ["jose"] = "python-jose[cryptography]>=3.3.0",
            ["passlib"] = "passlib[bcrypt]>=1.7.4",
            ["bcrypt"] = "bcrypt>=4.2.0",
            ["motor"] = "motor>=3.6.0",
            ["pymongo"] = "pymongo>=4.10.0",
            ["psycopg2"] = "psycopg2-binary>=2.9.9",
            ["asyncpg"] = "asyncpg>=0.29.0",
            ["slowapi"] = "slowapi>=0.1.9",
            ["email_validator"] = "email-validator>=2.0.0",
            ["pytest_asyncio"] = "pytest-asyncio>=0.23.0",
        };

    private static readonly IReadOnlyDictionary<string, string> PipNameAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["email-validator"] = "email-validator>=2.0.0",
            ["dotenv"] = "python-dotenv>=1.0.1",
            ["jose"] = "python-jose[cryptography]>=3.3.0",
            ["yaml"] = "pyyaml>=6.0.2",
        };

    public static int Sync(IList<GeneratedFile> files, string? buildLog = null)
    {
        var changed = SyncRequirements(files);
        changed += SyncFromBuildLog(files, buildLog);
        changed += MirrorRootRequirements(files);
        return changed;
    }

    public static bool ShouldSync(
        string? buildLog,
        IReadOnlyList<ErrorReport>? errors,
        IEnumerable<GeneratedFile> files)
    {
        if (files.Any(f => f.RelativePath.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase)))
        {
            if (files.Any(f => f.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase)
                               && (f.Content?.Contains("EmailStr", StringComparison.Ordinal) == true
                                   || f.Content?.Contains("slowapi", StringComparison.OrdinalIgnoreCase) == true)))
                return true;
        }

        if (errors is not null && errors.Any(e =>
                IsMissingPackageSignal($"{e.ErrorType} {e.Message} {e.FilePath}")))
            return true;

        return IsMissingPackageSignal(buildLog);
    }

    public static int SyncRequirements(IList<GeneratedFile> files)
    {
        var reqFiles = files
            .Where(f => f.RelativePath.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (reqFiles.Count == 0)
            return 0;

        var sources = files
            .Where(f => f.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (sources.Count == 0)
            return 0;

        var changed = 0;
        foreach (var reqFile in reqFiles)
        {
            var specs = CollectRequiredSpecs(sources);
            if (specs.Count == 0)
                continue;

            var idx = files.ToList().FindIndex(f =>
                f.RelativePath.Equals(reqFile.RelativePath, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                continue;

            if (TryMergeRequirementSpecs(files[idx].Content ?? string.Empty, specs, out var merged))
            {
                files[idx] = new GeneratedFile(files[idx].RelativePath, files[idx].Language, merged);
                changed++;
            }
        }

        return changed;
    }

    /// <summary>
    /// Adds packages referenced in build stderr (ModuleNotFoundError) to requirements.txt.
    /// </summary>
    public static int SyncFromBuildLog(IList<GeneratedFile> files, string? buildLog)
    {
        if (string.IsNullOrWhiteSpace(buildLog))
            return 0;

        var specs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in ModuleNotFound.Matches(buildLog))
        {
            var mod = match.Groups[1].Value.Split('.')[0];
            if (TryResolveRequirementSpec(mod, out var spec))
                specs.Add(spec);
        }

        if (specs.Count == 0)
            return 0;

        var changed = 0;
        foreach (var reqFile in files.Where(f =>
                     f.RelativePath.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            var idx = files.IndexOf(reqFile);
            if (TryMergeRequirementSpecs(files[idx].Content ?? string.Empty, specs, out var merged))
            {
                files[idx] = new GeneratedFile(reqFile.RelativePath, reqFile.Language, merged);
                changed++;
            }
        }

        return changed;
    }

    private static readonly HashSet<string> LocalImportRoots = new(StringComparer.OrdinalIgnoreCase)
    {
        "src", "app", "main", "tests", "test", "models", "schemas", "routers", "services",
        "database", "db", "core", "api", "conftest", "crmbackend", "backend", "lib", "pkg",
    };

    internal static bool TryResolveRequirementSpec(
        string moduleOrPackage,
        out string spec,
        bool allowGenericFallback = true)
    {
        spec = string.Empty;
        if (string.IsNullOrWhiteSpace(moduleOrPackage))
            return false;

        var root = moduleOrPackage.Split('.')[0];
        if (ModuleToRequirement.TryGetValue(root, out var mapped))
        {
            spec = mapped;
            return true;
        }

        var normalized = root.Replace('_', '-');
        if (PipNameAliases.TryGetValue(normalized, out var aliasSpec))
        {
            spec = aliasSpec;
            return true;
        }

        if (IsStdlibModule(root) || LocalImportRoots.Contains(root))
            return false;

        if (!allowGenericFallback)
            return false;

        spec = $"{normalized}>=0.0.0";
        return true;
    }

    private static HashSet<string> CollectRequiredSpecs(IEnumerable<GeneratedFile> sources)
    {
        var specs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in sources)
        {
            var content = file.Content ?? string.Empty;
            foreach (Match m in ImportFrom.Matches(content))
            {
                var mod = (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value).Split('.')[0];
                if (string.IsNullOrWhiteSpace(mod) || mod.StartsWith("_", StringComparison.Ordinal))
                    continue;

                if (TryResolveRequirementSpec(mod, out var spec, allowGenericFallback: false))
                    specs.Add(spec);
            }

            foreach (var (pattern, spec) in TransitiveByUsage)
            {
                if (pattern.IsMatch(content))
                    specs.Add(spec);
            }
        }

        return specs;
    }

    private static bool TryMergeRequirementSpecs(string text, IEnumerable<string> specs, out string result)
    {
        result = text;
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var pkg = line.Split(new[] { '=', '<', '>', '[', ';' }, 2)[0].Trim();
            if (!string.IsNullOrWhiteSpace(pkg))
                existing.Add(NormalizePkgName(pkg));
        }

        var changed = false;
        foreach (var spec in specs)
        {
            var pkgName = NormalizePkgName(spec.Split(new[] { '=', '<', '>', '[' }, 2)[0].Trim());
            if (existing.Contains(pkgName))
                continue;

            lines.Add(spec);
            existing.Add(pkgName);
            changed = true;
        }

        if (!changed)
            return false;

        result = string.Join(Environment.NewLine, lines) + Environment.NewLine;
        return true;
    }

    private static int MirrorRootRequirements(IList<GeneratedFile> files)
    {
        if (files.Any(f => f.RelativePath.Equals("requirements.txt", StringComparison.OrdinalIgnoreCase)))
            return 0;

        var nested = files.FirstOrDefault(f =>
            f.RelativePath.Equals("src/requirements.txt", StringComparison.OrdinalIgnoreCase)
            || f.RelativePath.Equals("backend/requirements.txt", StringComparison.OrdinalIgnoreCase));
        if (nested?.Content is null)
            return 0;

        files.Add(new GeneratedFile("requirements.txt", "text", nested.Content));
        return 1;
    }

    private static string NormalizePkgName(string name) =>
        name.Replace('_', '-').ToLowerInvariant();

    private static bool IsMissingPackageSignal(string? signal)
    {
        if (string.IsNullOrWhiteSpace(signal))
            return false;

        return signal.Contains("ModuleNotFoundError", StringComparison.OrdinalIgnoreCase)
               || signal.Contains("No module named", StringComparison.OrdinalIgnoreCase)
               || signal.Contains("email-validator", StringComparison.OrdinalIgnoreCase)
               || signal.Contains("email_validator", StringComparison.OrdinalIgnoreCase)
               || signal.Contains("slowapi", StringComparison.OrdinalIgnoreCase)
               || signal.Contains("missingpackage", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStdlibModule(string root) =>
        root is "os" or "sys" or "json" or "re" or "typing" or "datetime" or "pathlib" or "collections"
            or "functools" or "itertools" or "enum" or "abc" or "contextlib" or "dataclasses"
            or "asyncio" or "logging" or "uuid" or "io" or "copy" or "math" or "time";
}
