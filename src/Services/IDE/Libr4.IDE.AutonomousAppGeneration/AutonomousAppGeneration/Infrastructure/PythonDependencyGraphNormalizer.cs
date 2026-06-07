using System.Text;
using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic Python dependency graph repair: prefix normalization, import rewrite, symbol remap, module stubs.
/// Complements <see cref="PythonProjectLayoutNormalizer"/> (pytest/sys.path layer).
/// </summary>
public static class PythonDependencyGraphNormalizer
{
    public enum RecoveryAction
    {
        PrefixNormalize,
        RewriteImportToExistingSymbol,
        GenerateModuleStub,
        RemapSymbol
    }

    private static readonly Regex FromImportLine = new(
        @"^\s*from\s+(?<module>[\w.]+)\s+import\s+(?<symbols>.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex DefSymbol = new(
        @"^\s*def\s+(?<name>\w+)\s*\(",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex ClassSymbol = new(
        @"^\s*class\s+(?<name>\w+)\b",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly HashSet<string> InfrastructureModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "database", "db", "middleware", "auth", "dependencies", "deps", "config", "settings"
    };

    private static readonly Dictionary<string, string> KnownSymbolAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Customer"] = "CustomerDB",
        ["User"] = "UserDB",
        ["Order"] = "OrderDB"
    };

    public static int Normalize(
        IList<GeneratedFile> files,
        string? buildLog,
        IReadOnlyList<ErrorReport>? errors = null)
    {
        if (!ShouldNormalize(buildLog, errors, files))
            return 0;

        var packageRoot = ResolvePackageRoot(files);
        var changed = NormalizeImportPrefixes(files, packageRoot);
        changed += RemapSymbolsOnImportLines(files);
        changed += RewriteImportsToExistingSymbolModules(files, packageRoot);
        changed += GenerateMissingInfrastructureStubs(files, packageRoot, buildLog, errors);
        return changed;
    }

    public static bool ShouldNormalize(
        string? buildLog,
        IReadOnlyList<ErrorReport>? errors,
        IEnumerable<GeneratedFile> files)
    {
        var pyFiles = files.Where(f => f.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase)).ToList();
        if (pyFiles.Count == 0)
            return false;

        if (pyFiles.Any(f => (f.Content ?? string.Empty).Contains("src.app.", StringComparison.OrdinalIgnoreCase)
                             || (f.Content ?? string.Empty).Contains("from src.", StringComparison.OrdinalIgnoreCase)))
            return true;

        if (HasMissingLocalModuleImport(pyFiles, ResolvePackageRoot(files)))
            return true;

        if (errors is not null && errors.Any(IsLocalImportError))
            return true;

        if (string.IsNullOrWhiteSpace(buildLog))
            return false;

        return buildLog.Contains("ModuleNotFoundError", StringComparison.OrdinalIgnoreCase)
               || buildLog.Contains("ImportError", StringComparison.OrdinalIgnoreCase)
               || buildLog.Contains("No module named", StringComparison.OrdinalIgnoreCase)
               || buildLog.Contains("cannot import name", StringComparison.OrdinalIgnoreCase);
    }

    internal static string ResolvePackageRoot(IEnumerable<GeneratedFile> files)
    {
        var paths = files.Select(f => f.RelativePath.Replace('\\', '/')).ToList();
        if (paths.Any(p => p.StartsWith("src/app/", StringComparison.OrdinalIgnoreCase)
                           || p.Equals("src/main.py", StringComparison.OrdinalIgnoreCase)))
            return "src";

        if (paths.Any(p => p.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)))
            return "backend";

        return string.Empty;
    }

    internal static string ModuleToRelativePath(string module, string packageRoot)
    {
        var dotted = module.Replace('.', '/');
        if (!string.IsNullOrEmpty(packageRoot)
            && !dotted.StartsWith(packageRoot + "/", StringComparison.OrdinalIgnoreCase))
            return $"{packageRoot.TrimEnd('/')}/{dotted}.py";

        return $"{dotted}.py";
    }

    private static int NormalizeImportPrefixes(IList<GeneratedFile> files, string packageRoot)
    {
        if (string.IsNullOrEmpty(packageRoot))
            return 0;

        var stripPrefix = packageRoot + ".";
        var changed = 0;
        for (var i = 0; i < files.Count; i++)
        {
            if (!files[i].RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = files[i].Content ?? string.Empty;
            var updated = content;

            updated = Regex.Replace(
                updated,
                $@"^\s*from\s+{Regex.Escape(stripPrefix)}([\w.]+)\s+import",
                $"from $1 import",
                RegexOptions.Multiline);
            updated = Regex.Replace(
                updated,
                $@"^\s*import\s+{Regex.Escape(stripPrefix)}([\w.]+)\s*$",
                "import $1",
                RegexOptions.Multiline);

            if (FileExists(files, ModuleToRelativePath("main", packageRoot))
                && !FileExists(files, ModuleToRelativePath("app.main", packageRoot)))
            {
                updated = Regex.Replace(
                    updated,
                    @"^\s*from\s+app\.main\s+import",
                    "from main import",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase);
            }

            if (string.Equals(updated, content, StringComparison.Ordinal))
                continue;

            files[i] = new GeneratedFile(files[i].RelativePath, files[i].Language, updated);
            changed++;
        }

        return changed;
    }

    private static int RemapSymbolsOnImportLines(IList<GeneratedFile> files)
    {
        var symbolIndex = BuildSymbolIndex(files);
        var changed = 0;

        for (var i = 0; i < files.Count; i++)
        {
            if (!files[i].RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = files[i].Content ?? string.Empty;
            var updated = content;
            foreach (Match match in FromImportLine.Matches(content))
            {
                var module = match.Groups["module"].Value;
                var symbolsPart = match.Groups["symbols"].Value;
                var symbols = ParseImportSymbols(symbolsPart);
                var remapped = false;
                var newSymbols = new List<string>();

                foreach (var symbol in symbols)
                {
                    var name = symbol.Trim();
                    if (name.Equals("as", StringComparison.OrdinalIgnoreCase))
                    {
                        newSymbols.Add(name);
                        continue;
                    }

                    if (symbolIndex.ContainsKey(name))
                    {
                        newSymbols.Add(name);
                        continue;
                    }

                    if (KnownSymbolAliases.TryGetValue(name, out var alias) && symbolIndex.ContainsKey(alias))
                    {
                        newSymbols.Add(alias);
                        remapped = true;
                        continue;
                    }

                    newSymbols.Add(name);
                }

                if (!remapped)
                    continue;

                var newLine = $"from {module} import {string.Join(", ", newSymbols)}";
                updated = updated.Replace(match.Value, newLine);
            }

            if (string.Equals(updated, content, StringComparison.Ordinal))
                continue;

            files[i] = new GeneratedFile(files[i].RelativePath, files[i].Language, updated);
            changed++;
        }

        return changed;
    }

    private static int RewriteImportsToExistingSymbolModules(
        IList<GeneratedFile> files,
        string packageRoot)
    {
        var symbolIndex = BuildSymbolIndex(files);
        var changed = 0;

        for (var i = 0; i < files.Count; i++)
        {
            if (!files[i].RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = files[i].Content ?? string.Empty;
            var updated = content;

            foreach (Match match in FromImportLine.Matches(content))
            {
                var module = match.Groups["module"].Value;
                var relPath = ModuleToRelativePath(module, packageRoot);
                if (FileExists(files, relPath))
                    continue;

                foreach (var symbol in ParseImportSymbols(match.Groups["symbols"].Value))
                {
                    var sym = symbol.Trim();
                    if (sym.Equals("as", StringComparison.OrdinalIgnoreCase) || sym.StartsWith('('))
                        continue;

                    if (!symbolIndex.TryGetValue(sym, out var ownerModule))
                        continue;

                    if (string.Equals(ownerModule, module, StringComparison.Ordinal))
                        continue;

                    var oldLine = match.Value;
                    var newLine = Regex.Replace(
                        oldLine,
                        $@"from\s+{Regex.Escape(module)}\s+import",
                        $"from {ownerModule} import",
                        RegexOptions.IgnoreCase);
                    updated = updated.Replace(oldLine, newLine);
                    break;
                }
            }

            if (string.Equals(updated, content, StringComparison.Ordinal))
                continue;

            files[i] = new GeneratedFile(files[i].RelativePath, files[i].Language, updated);
            changed++;
        }

        return changed;
    }

    private static int GenerateMissingInfrastructureStubs(
        IList<GeneratedFile> files,
        string packageRoot,
        string? buildLog,
        IReadOnlyList<ErrorReport>? errors)
    {
        var missingModules = CollectMissingImportedModules(files, packageRoot, buildLog, errors);
        var changed = 0;

        foreach (var module in missingModules)
        {
            var relPath = ModuleToRelativePath(module, packageRoot);
            if (FileExists(files, relPath))
                continue;

            var leaf = module.Split('.').LastOrDefault() ?? module;
            if (!InfrastructureModules.Contains(leaf))
                continue;

            var stub = leaf.Equals("database", StringComparison.OrdinalIgnoreCase)
                       || leaf.Equals("db", StringComparison.OrdinalIgnoreCase)
                ? BuildDatabaseStub(files, packageRoot, module)
                : BuildMinimalPackageStub(module);

            if (string.IsNullOrWhiteSpace(stub))
                continue;

            files.Add(new GeneratedFile(relPath, "python", stub));
            changed++;
        }

        return changed;
    }

    internal static string BuildDatabaseStub(
        IEnumerable<GeneratedFile> files,
        string packageRoot,
        string targetModule)
    {
        var modelsModule = ResolveExistingModule(files, packageRoot, "app.models", "models");
        var baseImport = modelsModule is not null ? $"from {modelsModule} import Base" : string.Empty;
        var packagePrefix = targetModule.Contains('.')
            ? targetModule[..targetModule.LastIndexOf('.')]
            : "app";

        var sb = new StringBuilder();
        sb.AppendLine("\"\"\"SQLAlchemy session factory (deterministic infrastructure stub).\"\"\"");
        sb.AppendLine();
        sb.AppendLine("from collections.abc import Generator");
        sb.AppendLine();
        sb.AppendLine("from sqlalchemy import create_engine");
        sb.AppendLine("from sqlalchemy.orm import Session, sessionmaker");
        if (!string.IsNullOrEmpty(baseImport))
        {
            sb.AppendLine(baseImport);
            sb.AppendLine();
        }

        sb.AppendLine("DATABASE_URL = \"sqlite:///./app.db\"");
        sb.AppendLine("engine = create_engine(DATABASE_URL, connect_args={\"check_same_thread\": False})");
        sb.AppendLine("SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("def get_db() -> Generator[Session, None, None]:");
        sb.AppendLine("    db = SessionLocal()");
        sb.AppendLine("    try:");
        sb.AppendLine("        yield db");
        sb.AppendLine("    finally:");
        sb.AppendLine("        db.close()");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(baseImport))
        {
            sb.AppendLine();
            sb.AppendLine("def init_db() -> None:");
            sb.AppendLine("    Base.metadata.create_all(bind=engine)");
        }

        return sb.ToString();
    }

    private static string BuildMinimalPackageStub(string module) =>
        $"\"\"\"Deterministic stub for missing local module '{module}'.\"\"\"\n";

    private static Dictionary<string, string> BuildSymbolIndex(IEnumerable<GeneratedFile> files)
    {
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files.Where(f => f.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase)))
        {
            var content = file.Content ?? string.Empty;
            var module = RelativePathToModule(file.RelativePath.Replace('\\', '/'), ResolvePackageRoot(files));
            if (string.IsNullOrEmpty(module))
                continue;

            foreach (Match m in DefSymbol.Matches(content))
                index.TryAdd(m.Groups["name"].Value, module);

            foreach (Match m in ClassSymbol.Matches(content))
                index.TryAdd(m.Groups["name"].Value, module);
        }

        return index;
    }

    private static string RelativePathToModule(string relativePath, string packageRoot)
    {
        var path = relativePath;
        if (path.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            path = path[..^3];

        if (!string.IsNullOrEmpty(packageRoot)
            && path.StartsWith(packageRoot + "/", StringComparison.OrdinalIgnoreCase))
            path = path[(packageRoot.Length + 1)..];

        return path.Replace('/', '.');
    }

    private static HashSet<string> CollectMissingImportedModules(
        IEnumerable<GeneratedFile> files,
        string packageRoot,
        string? buildLog,
        IReadOnlyList<ErrorReport>? errors)
    {
        var missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files.Where(f => f.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase)))
        {
            var content = file.Content ?? string.Empty;
            foreach (Match match in FromImportLine.Matches(content))
            {
                var module = match.Groups["module"].Value;
                if (IsStdlibOrThirdParty(module))
                    continue;

                var rel = ModuleToRelativePath(module, packageRoot);
                if (!FileExists(files, rel))
                    missing.Add(module);
            }
        }

        foreach (var extracted in ExtractMissingModulesFromSignals(buildLog, errors))
            missing.Add(extracted);

        return missing;
    }

    private static IEnumerable<string> ExtractMissingModulesFromSignals(
        string? buildLog,
        IReadOnlyList<ErrorReport>? errors)
    {
        var blob = buildLog ?? string.Empty;
        if (errors is not null)
            blob += "\n" + string.Join('\n', errors.Select(e => $"{e.Message} {e.FilePath}"));

        foreach (Match m in Regex.Matches(blob, @"No module named '([^']+)'", RegexOptions.IgnoreCase))
            yield return m.Groups[1].Value;

        foreach (Match m in Regex.Matches(blob, @"ModuleNotFoundError:\s*No module named '?([\w.]+)'?", RegexOptions.IgnoreCase))
            yield return m.Groups[1].Value;
    }

    private static bool HasMissingLocalModuleImport(IList<GeneratedFile> files, string packageRoot)
    {
        foreach (var file in files)
        {
            if (!file.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (Match match in FromImportLine.Matches(file.Content ?? string.Empty))
            {
                var module = match.Groups["module"].Value;
                if (IsStdlibOrThirdParty(module))
                    continue;

                if (!FileExists(files, ModuleToRelativePath(module, packageRoot)))
                    return true;
            }
        }

        return false;
    }

    private static bool FileExists(IEnumerable<GeneratedFile> files, string relativePath) =>
        files.Any(f => f.RelativePath.Replace('\\', '/')
            .Equals(relativePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));

    private static string? ResolveExistingModule(
        IEnumerable<GeneratedFile> files,
        string packageRoot,
        params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (FileExists(files, ModuleToRelativePath(candidate, packageRoot)))
                return candidate;
        }

        return null;
    }

    private static List<string> ParseImportSymbols(string symbolsPart)
    {
        var results = new List<string>();
        foreach (var token in symbolsPart.Split(','))
        {
            var t = token.Trim();
            if (string.IsNullOrEmpty(t))
                continue;
            results.Add(t);
        }

        return results;
    }

    private static bool IsStdlibOrThirdParty(string module)
    {
        if (string.IsNullOrWhiteSpace(module))
            return true;

        var root = module.Split('.')[0];
        return root is "fastapi" or "pydantic" or "sqlalchemy" or "pytest" or "httpx" or "uvicorn"
               or "typing" or "datetime" or "os" or "sys" or "json" or "re" or "collections"
               or "contextlib" or "functools" or "enum" or "pathlib" or "starlette";
    }

    private static bool IsLocalImportError(ErrorReport error)
    {
        var signal = $"{error.ErrorType} {error.Message} {error.FilePath}";
        return signal.Contains("ModuleNotFoundError", StringComparison.OrdinalIgnoreCase)
               || signal.Contains("ImportError", StringComparison.OrdinalIgnoreCase)
               || signal.Contains("cannot import name", StringComparison.OrdinalIgnoreCase);
    }
}
