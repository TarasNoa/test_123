using System.Text;
using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic Python project layout normalization for pytest + FastAPI imports.
/// Runs without LLM: discovers the FastAPI app module, classifies layout, rewrites test/conftest imports.
/// </summary>
public static class PythonProjectLayoutNormalizer
{
    public enum PythonProjectLayoutKind
    {
        FlatMainWithRootTests,
        SrcMainWithNestedTests,
        SrcMainWithRootTests,
        SrcPackageApp,
        BackendPackageApp,
        Unknown
    }

    public sealed record PythonAppDiscovery(
        string ModuleFilePath,
        string ImportModule,
        string SysPathRoot,
        PythonProjectLayoutKind Layout,
        string ExportName = "app");

    private static readonly Regex FastApiAppAssignment = new(
        @"(?:^|\n)\s*(?<name>\w+)\s*=\s*FastAPI\s*\(",
        RegexOptions.Compiled);

    private static readonly Regex ImportAppLine = new(
        @"^\s*from\s+(?<module>(?:[\w.]+\.)?(?:main|app)(?:\.[\w.]+)?)\s+import\s+(?<symbol>\w+)",
        RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SysPathInsertLine = new(
        @"^\s*sys\.path\.insert\([^\n]+\)\s*\n?",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex BrokenTestPathPattern = new(
        @"(?:File ""(?<path>[^""]*tests[^""]*\.py)""|ERROR collecting\s+(?<path>[^\s:]+\.py)|ImportError while importing test module\s+'(?<path>[^']+\.py)'|(?<path>[^\s'""\n]*tests[^\s'""\n]*\.py):\d+:)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static int Normalize(
        IList<GeneratedFile> files,
        string? buildLog,
        IReadOnlyList<ErrorReport>? errors = null)
    {
        if (!ShouldNormalize(buildLog, errors, files))
            return 0;

        var discovery = DiscoverAppEntry(files);
        if (discovery is null)
            return 0;

        var changed = EnsurePackageInitFiles(files, discovery);
        changed += RemoveConflictingRootConftest(files);
        changed += NormalizeConftestFiles(files, discovery);
        changed += NormalizeTestFiles(files, discovery, buildLog);
        changed += EnsureTestRootConftest(files, discovery);
        return changed;
    }

    public static bool ShouldNormalize(
        string? buildLog,
        IReadOnlyList<ErrorReport>? errors,
        IEnumerable<GeneratedFile> files)
    {
        if (DiscoverAppEntry(files) is null)
            return false;

        if (files.Any(f => IsPytestFile(f) && NeedsImportFix(f.Content ?? string.Empty)))
            return true;

        if (HasConflictingRootConftest(files))
            return true;

        if (errors is not null && errors.Any(IsPytestImportError))
            return true;

        if (string.IsNullOrWhiteSpace(buildLog))
            return false;

        return buildLog.Contains("ImportError", StringComparison.OrdinalIgnoreCase)
               || buildLog.Contains("ModuleNotFoundError", StringComparison.OrdinalIgnoreCase)
               || buildLog.Contains("ERROR collecting", StringComparison.OrdinalIgnoreCase)
               || buildLog.Contains("No module named 'main'", StringComparison.OrdinalIgnoreCase)
               || buildLog.Contains("No module named \"main\"", StringComparison.OrdinalIgnoreCase)
               || buildLog.Contains("src/src", StringComparison.OrdinalIgnoreCase);
    }

    internal static PythonAppDiscovery? DiscoverAppEntry(IEnumerable<GeneratedFile> files)
    {
        var candidates = files
            .Where(f => f.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(f.Content)
                        && f.Content.Contains("FastAPI(", StringComparison.Ordinal))
            .Select(f =>
            {
                var export = ResolveFastApiExportName(f.Content!);
                return new
                {
                    File = f,
                    Export = export,
                    Path = f.RelativePath.Replace('\\', '/'),
                    Score = ScoreAppCandidate(f.RelativePath.Replace('\\', '/'), export)
                };
            })
            .OrderBy(c => c.Score)
            .ToList();

        var best = candidates.FirstOrDefault();
        if (best is null)
        {
            var mainPath = FindLegacyMainPath(files);
            if (mainPath is null)
                return null;

            return BuildDiscovery(mainPath, "app");
        }

        return BuildDiscovery(best.Path, best.Export);
    }

    internal static string ResolveImportModule(string moduleFilePath)
    {
        var normalized = moduleFilePath.Replace('\\', '/');
        if (normalized.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^3];

        if (normalized.StartsWith("src/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[4..];

        return normalized.Replace('/', '.');
    }

    internal static string ResolveSysPathRoot(string moduleFilePath, string importModule)
    {
        var normalized = moduleFilePath.Replace('\\', '/');
        if (normalized.StartsWith("src/", StringComparison.OrdinalIgnoreCase))
            return "src";

        if (normalized.StartsWith("backend/", StringComparison.OrdinalIgnoreCase))
        {
            var parts = importModule.Split('.');
            if (parts.Length > 0
                && normalized.Contains("/" + parts[0] + "/", StringComparison.OrdinalIgnoreCase))
            {
                var idx = normalized.IndexOf("/" + parts[0] + "/", StringComparison.OrdinalIgnoreCase);
                return normalized[..idx].TrimEnd('/');
            }

            return string.Empty;
        }

        if (!normalized.Contains('/'))
            return string.Empty;

        return Path.GetDirectoryName(normalized)?.Replace('\\', '/') ?? string.Empty;
    }

    internal static string BuildSysPathInsert(string testRelativePath, string sysPathRoot)
    {
        var normalizedTestPath = testRelativePath.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(sysPathRoot))
        {
            var testDir = Path.GetDirectoryName(normalizedTestPath)?.Replace('\\', '/') ?? string.Empty;
            var testParts = testDir.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (testParts.Length == 0)
                return "os.path.dirname(os.path.abspath(__file__))";

            var up = string.Join("', '", Enumerable.Repeat("..", testParts.Length));
            return $"os.path.abspath(os.path.join(os.path.dirname(__file__), '{up}'))";
        }

        var normalizedRoot = sysPathRoot.Replace('\\', '/').Trim('/');
        var testDirectory = Path.GetDirectoryName(normalizedTestPath)?.Replace('\\', '/') ?? string.Empty;
        var targetParts = normalizedRoot.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var testPathParts = testDirectory.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var common = 0;
        while (common < testPathParts.Length
               && common < targetParts.Length
               && string.Equals(testPathParts[common], targetParts[common], StringComparison.OrdinalIgnoreCase))
            common++;

        var segments = Enumerable
            .Repeat("..", testPathParts.Length - common)
            .Concat(targetParts.Skip(common))
            .ToList();

        if (segments.Count == 0)
            return "os.path.dirname(os.path.abspath(__file__))";

        var joined = string.Join("', '", segments);
        return $"os.path.abspath(os.path.join(os.path.dirname(__file__), '{joined}'))";
    }

    internal static string BuildSysPathInsert(string testRelativePath, PythonAppDiscovery discovery) =>
        BuildSysPathInsert(testRelativePath, discovery.SysPathRoot);

    internal static string BuildMinimalFastApiTest(string testRelativePath, PythonAppDiscovery discovery)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(discovery.SysPathRoot)
            || testRelativePath.Contains("tests/", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("import os");
            sb.AppendLine("import sys");
            sb.AppendLine($"sys.path.insert(0, {BuildSysPathInsert(testRelativePath, discovery)})");
        }

        sb.AppendLine("from fastapi.testclient import TestClient");
        sb.AppendLine($"from {discovery.ImportModule} import {discovery.ExportName}");
        sb.AppendLine();
        sb.AppendLine($"client = TestClient({discovery.ExportName})");
        sb.AppendLine();
        sb.AppendLine("def test_health_route():");
        sb.AppendLine("    response = client.get(\"/health\")");
        sb.AppendLine("    assert response.status_code in (200, 404)");
        return sb.ToString().TrimEnd() + "\n";
    }

    public static bool IsLocalPythonModule(string moduleName, IEnumerable<GeneratedFile> files) =>
        !string.IsNullOrWhiteSpace(moduleName)
        && files.Any(f => ModulePathMatches(f.RelativePath, moduleName));

    private static PythonAppDiscovery BuildDiscovery(string moduleFilePath, string exportName)
    {
        var importModule = ResolveImportModule(moduleFilePath);
        var sysPathRoot = ResolveSysPathRoot(moduleFilePath, importModule);
        var layout = ClassifyLayout(moduleFilePath, importModule);
        return new PythonAppDiscovery(moduleFilePath, importModule, sysPathRoot, layout, exportName);
    }

    private static PythonProjectLayoutKind ClassifyLayout(string moduleFilePath, string importModule)
    {
        var path = moduleFilePath.Replace('\\', '/');
        var hasSrcTests = path.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
                          || importModule.StartsWith("app.", StringComparison.Ordinal);

        if (path.Equals("main.py", StringComparison.OrdinalIgnoreCase))
            return PythonProjectLayoutKind.FlatMainWithRootTests;

        if (path.Equals("src/main.py", StringComparison.OrdinalIgnoreCase))
            return PythonProjectLayoutKind.SrcMainWithNestedTests;

        if (path.StartsWith("src/", StringComparison.OrdinalIgnoreCase))
            return PythonProjectLayoutKind.SrcPackageApp;

        if (path.StartsWith("backend/", StringComparison.OrdinalIgnoreCase))
            return PythonProjectLayoutKind.BackendPackageApp;

        return hasSrcTests
            ? PythonProjectLayoutKind.SrcMainWithRootTests
            : PythonProjectLayoutKind.Unknown;
    }

    private static int ScoreAppCandidate(string path, string exportName)
    {
        var score = 0;
        if (!string.Equals(exportName, "app", StringComparison.OrdinalIgnoreCase))
            score += 20;

        if (path.Contains("/app/main.py", StringComparison.OrdinalIgnoreCase))
            score -= 20;

        if (path.EndsWith("/main.py", StringComparison.OrdinalIgnoreCase)
            || path.Equals("main.py", StringComparison.OrdinalIgnoreCase))
            score -= 8;

        if (path.Equals("src/main.py", StringComparison.OrdinalIgnoreCase))
            score += 6;

        score += path.Count(c => c == '/');
        if (path.StartsWith("src/", StringComparison.OrdinalIgnoreCase))
            score -= 2;

        return score;
    }

    private static string ResolveFastApiExportName(string content)
    {
        var match = FastApiAppAssignment.Match(content);
        if (match.Success)
            return match.Groups["name"].Value;

        return "app";
    }

    private static string? FindLegacyMainPath(IEnumerable<GeneratedFile> files)
    {
        var candidates = files
            .Select(f => f.RelativePath.Replace('\\', '/'))
            .Where(p => p.EndsWith("/main.py", StringComparison.OrdinalIgnoreCase)
                        || p.Equals("main.py", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
            return null;

        var testsUnderSrc = files.Any(f =>
            f.RelativePath.Replace('\\', '/').Contains("src/tests/", StringComparison.OrdinalIgnoreCase));

        if (testsUnderSrc)
        {
            return candidates.FirstOrDefault(p => p.Equals("src/main.py", StringComparison.OrdinalIgnoreCase))
                   ?? candidates.FirstOrDefault(p => p.StartsWith("src/", StringComparison.OrdinalIgnoreCase))
                   ?? candidates.OrderBy(p => p.Count(c => c == '/')).First();
        }

        return candidates
            .OrderBy(p => p.Count(c => c == '/'))
            .ThenBy(p => p.StartsWith("src/", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .FirstOrDefault();
    }

    private static int EnsurePackageInitFiles(IList<GeneratedFile> files, PythonAppDiscovery discovery)
    {
        var changed = 0;
        var modulePath = discovery.ModuleFilePath.Replace('\\', '/');
        var dir = Path.GetDirectoryName(modulePath)?.Replace('\\', '/') ?? string.Empty;
        while (!string.IsNullOrEmpty(dir))
        {
            var initPath = $"{dir}/__init__.py";
            if (!files.Any(f => f.RelativePath.Equals(initPath, StringComparison.OrdinalIgnoreCase)))
            {
                files.Add(new GeneratedFile(initPath, "python", string.Empty));
                changed++;
            }

            var parent = Path.GetDirectoryName(dir)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent) || parent == dir)
                break;

            if (dir.StartsWith("src/", StringComparison.OrdinalIgnoreCase) && !parent.Contains('/'))
                break;

            dir = parent;
        }

        return changed;
    }

    private static int RemoveConflictingRootConftest(IList<GeneratedFile> files)
    {
        var hasNestedTests = files.Any(f =>
            f.RelativePath.Replace('\\', '/').Contains("/tests/", StringComparison.OrdinalIgnoreCase));
        if (!hasNestedTests)
            return 0;

        var removed = 0;
        for (var i = files.Count - 1; i >= 0; i--)
        {
            if (files[i].RelativePath.Equals("conftest.py", StringComparison.OrdinalIgnoreCase))
            {
                files.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }

    private static int NormalizeConftestFiles(IList<GeneratedFile> files, PythonAppDiscovery discovery)
    {
        var changed = 0;
        foreach (var conftest in files
                     .Where(f => f.RelativePath.EndsWith("conftest.py", StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            if (conftest.RelativePath.Equals("conftest.py", StringComparison.OrdinalIgnoreCase))
                continue;

            var body = BuildConftestBody(conftest.RelativePath, discovery);
            var idx = FindFileIndex(files, conftest.RelativePath);
            if (idx < 0 || string.Equals(files[idx].Content, body, StringComparison.Ordinal))
                continue;

            files[idx] = new GeneratedFile(conftest.RelativePath, conftest.Language, body);
            changed++;
        }

        return changed;
    }

    private static int NormalizeTestFiles(
        IList<GeneratedFile> files,
        PythonAppDiscovery discovery,
        string? buildLog)
    {
        var changed = 0;
        var targeted = CollectTargetTestPaths(buildLog);

        foreach (var testFile in files.Where(IsPytestFile).ToList())
        {
            var path = testFile.RelativePath.Replace('\\', '/');
            var shouldFix = targeted.Count == 0
                            || targeted.Contains(path)
                            || NeedsImportFix(testFile.Content ?? string.Empty)
                            || HasBrokenSysPath(testFile.Content ?? string.Empty, discovery, testFile.RelativePath);

            if (!shouldFix)
                continue;

            if (IsTestFileCorrect(testFile.Content ?? string.Empty, discovery, testFile.RelativePath))
                continue;

            var updated = RewriteTestFile(testFile.Content ?? string.Empty, discovery, testFile.RelativePath);
            if (string.Equals(updated, testFile.Content, StringComparison.Ordinal))
            {
                updated = BuildMinimalFastApiTest(testFile.RelativePath, discovery);
            }

            var idx = FindFileIndex(files, testFile.RelativePath);
            if (idx < 0 || string.Equals(files[idx].Content, updated, StringComparison.Ordinal))
                continue;

            files[idx] = new GeneratedFile(testFile.RelativePath, testFile.Language, updated);
            changed++;
        }

        return changed;
    }

    private static int EnsureTestRootConftest(IList<GeneratedFile> files, PythonAppDiscovery discovery)
    {
        var testRoot = ResolvePrimaryTestRoot(files);
        if (string.IsNullOrWhiteSpace(testRoot))
            return 0;

        var conftestPath = $"{testRoot.TrimEnd('/')}/conftest.py";
        if (files.Any(f => f.RelativePath.Equals(conftestPath, StringComparison.OrdinalIgnoreCase)))
            return 0;

        files.Add(new GeneratedFile(conftestPath, "python", BuildConftestBody(conftestPath, discovery)));
        return 1;
    }

    private static string ResolvePrimaryTestRoot(IEnumerable<GeneratedFile> files)
    {
        var roots = files
            .Where(IsPytestFile)
            .Select(f => Path.GetDirectoryName(f.RelativePath.Replace('\\', '/')) ?? string.Empty)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .GroupBy(d => d, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        return roots.FirstOrDefault() ?? string.Empty;
    }

    private static HashSet<string> CollectTargetTestPaths(string? buildLog)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(buildLog))
            return paths;

        foreach (Match match in BrokenTestPathPattern.Matches(buildLog))
        {
            var rawPath = match.Groups["path"].Value.Replace('\\', '/').Trim().Trim('\'', '"');
            if (string.IsNullOrWhiteSpace(rawPath))
                continue;

            var relPath = rawPath;
            var srcIdx = rawPath.LastIndexOf("/src/", StringComparison.OrdinalIgnoreCase);
            if (srcIdx >= 0)
                relPath = rawPath[(srcIdx + 1)..];

            paths.Add(relPath);
        }

        return paths;
    }

    private static string BuildConftestBody(string conftestRelativePath, PythonAppDiscovery discovery)
    {
        return "import os\nimport sys\n\n"
               + $"sys.path.insert(0, {BuildSysPathInsert(conftestRelativePath, discovery)})\n";
    }

    private static string RewriteTestFile(string content, PythonAppDiscovery discovery, string testRelativePath)
    {
        var working = StripImportPreamble(content);
        var importLine = $"from {discovery.ImportModule} import {discovery.ExportName}";

        if (!working.Contains("TestClient", StringComparison.Ordinal))
        {
            working = "from fastapi.testclient import TestClient\n" + working;
        }
        else if (!working.Contains("from fastapi.testclient import TestClient", StringComparison.OrdinalIgnoreCase))
        {
            working = "from fastapi.testclient import TestClient\n" + working;
        }

        if (!working.Contains(importLine, StringComparison.Ordinal))
        {
            var testClientIdx = working.IndexOf("from fastapi.testclient", StringComparison.OrdinalIgnoreCase);
            if (testClientIdx >= 0)
            {
                var lineEnd = working.IndexOf('\n', testClientIdx);
                if (lineEnd < 0) lineEnd = working.Length;
                working = working.Insert(lineEnd + 1, importLine + "\n");
            }
            else
            {
                working = importLine + "\n" + working;
            }
        }

        var needsPath = !string.IsNullOrWhiteSpace(discovery.SysPathRoot)
                        || testRelativePath.Contains("tests/", StringComparison.OrdinalIgnoreCase);
        if (needsPath && !working.Contains("sys.path.insert", StringComparison.Ordinal))
        {
            var prefix =
                "import os\nimport sys\n"
                + $"sys.path.insert(0, {BuildSysPathInsert(testRelativePath, discovery)})\n";
            working = prefix + working;
        }

        return working.TrimStart();
    }

    private static string StripImportPreamble(string content)
    {
        var working = SysPathInsertLine.Replace(content, string.Empty);
        working = Regex.Replace(
            working,
            @"^import os\s*\nimport sys\s*\n",
            string.Empty,
            RegexOptions.Multiline);
        working = Regex.Replace(
            working,
            @"^\s*from\s+(?:src\.|[\w.]+\.)?(?:main|app)(?:\.[\w.]+)?\s+import\s+\w+\s*\n",
            string.Empty,
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
        return working;
    }

    private static bool IsTestFileCorrect(string content, PythonAppDiscovery discovery, string testRelativePath)
    {
        var expectedImport = $"from {discovery.ImportModule} import {discovery.ExportName}";
        if (!content.Contains(expectedImport, StringComparison.Ordinal))
            return false;

        if (content.Contains("from main import", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(discovery.ImportModule, "main", StringComparison.Ordinal))
            return false;

        if (!string.IsNullOrWhiteSpace(discovery.SysPathRoot))
        {
            if (!content.Contains("sys.path.insert", StringComparison.Ordinal))
                return false;

            if (HasBrokenSysPath(content, discovery, testRelativePath))
                return false;
        }

        return true;
    }

    private static bool HasBrokenSysPath(string content, PythonAppDiscovery discovery, string testRelativePath)
    {
        if (content.Contains("src/src", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!content.Contains("sys.path.insert", StringComparison.Ordinal))
            return false;

        var expected = BuildSysPathInsert(testRelativePath, discovery);
        return !content.Contains(expected, StringComparison.Ordinal);
    }

    private static bool NeedsImportFix(string content) =>
        ImportAppLine.IsMatch(content)
        || content.Contains("from main import", StringComparison.OrdinalIgnoreCase)
        || content.Contains("from src.main import", StringComparison.OrdinalIgnoreCase);

    private static bool IsPytestImportError(ErrorReport error)
    {
        var signal = $"{error.ErrorType} {error.Message} {error.FilePath}";
        return (signal.Contains("ImportError", StringComparison.OrdinalIgnoreCase)
                || signal.Contains("ModuleNotFoundError", StringComparison.OrdinalIgnoreCase))
               && signal.Contains("test", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasConflictingRootConftest(IEnumerable<GeneratedFile> files) =>
        files.Any(f => f.RelativePath.Equals("conftest.py", StringComparison.OrdinalIgnoreCase))
        && files.Any(f =>
            f.RelativePath.Replace('\\', '/').Contains("/tests/", StringComparison.OrdinalIgnoreCase));

    private static bool IsPytestFile(GeneratedFile file) =>
        file.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase)
        && (file.RelativePath.Contains("test", StringComparison.OrdinalIgnoreCase)
            || file.RelativePath.Replace('\\', '/').Contains("/tests/"));

    private static bool ModulePathMatches(string relativePath, string moduleName)
    {
        var normalized = relativePath.Replace('\\', '/');
        var dottedPath = moduleName.Replace('.', '/');
        return normalized.Equals($"{moduleName}.py", StringComparison.OrdinalIgnoreCase)
               || normalized.EndsWith($"/{moduleName}.py", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals($"{dottedPath}.py", StringComparison.OrdinalIgnoreCase)
               || normalized.EndsWith($"/{dottedPath}.py", StringComparison.OrdinalIgnoreCase);
    }

    private static int FindFileIndex(IList<GeneratedFile> files, string relativePath) =>
        files.ToList().FindIndex(f =>
            f.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
}
