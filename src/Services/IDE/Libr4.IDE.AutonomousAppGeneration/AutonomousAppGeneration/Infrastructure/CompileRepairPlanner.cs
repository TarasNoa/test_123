using System.Text;
using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Clusters build errors, picks a root cause, and prepares a focused fixer payload
/// (investigator mode: log excerpt + primary file + dependency context).
/// </summary>
public static class CompileRepairPlanner
{
    private static readonly Regex MavenErrorLine = new(
        @"\[ERROR\]\s+(?<path>[^\s:]+\.(?:java|xml|kt)):\[(?<line>\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public sealed record RepairPlan(
        IReadOnlyList<ErrorReport> FixerErrors,
        ErrorReport RootCause,
        int TotalErrorCount,
        int ClusterCount,
        string BuildLogExcerpt,
        string RootCauseCategory,
        CompileErrorAnalyzer.CompileErrorAnalysis? SymbolAnalysis = null);

    public static RepairPlan BuildPlan(
        ExecutionResult execution,
        IReadOnlyList<GeneratedFile> files,
        IReadOnlyList<ErrorReport> errors,
        GenerationPlan? plan = null)
    {
        if (errors.Count == 0)
        {
            var fallback = new ErrorReport(
                "BuildOrRuntimeError",
                "Build failed with no structured errors.",
                "Inspect build log and fix the first root cause (manifest, pom, or entry point).",
                diagnosingAgent: "CompileRepairPlanner");
            return new RepairPlan(
                new[] { fallback },
                fallback,
                0,
                0,
                ExtractBuildLogExcerpt(execution),
                "unknown");
        }

        var logExcerpt = ExtractBuildLogExcerpt(execution);
        var enriched = EnrichFromBuildLog(errors, logExcerpt, files);
        var clusters = ClusterErrors(enriched);
        var root = SelectRootCause(enriched, logExcerpt, files);
        var symbolAnalysis = plan is not null
            ? CompileErrorAnalyzer.Analyze(root, logExcerpt, files, plan)
            : null;
        var category = ClassifyRootCause(root, logExcerpt, symbolAnalysis);

        var fixerErrors = new List<ErrorReport> { EnrichError(root, logExcerpt, category, isRoot: true) };
        foreach (var related in SelectRelatedErrors(enriched, root, max: 4))
        {
            if (fixerErrors.Any(e => string.Equals(e.FilePath, related.FilePath, StringComparison.OrdinalIgnoreCase)
                                    && string.Equals(e.Message, related.Message, StringComparison.OrdinalIgnoreCase)))
                continue;
            fixerErrors.Add(EnrichError(related, logExcerpt, category, isRoot: false));
        }

        return new RepairPlan(
            fixerErrors,
            root,
            enriched.Count,
            clusters.Count,
            logExcerpt,
            category,
            symbolAnalysis);
    }

    public static string PlannerCategoryFromAnalysis(CompileErrorAnalyzer.CompileErrorAnalysis? analysis) =>
        analysis?.Kind switch
        {
            CompileErrorAnalyzer.CompileFixKind.MissingClass => "missing_class",
            CompileErrorAnalyzer.CompileFixKind.MissingInterface => "missing_interface",
            CompileErrorAnalyzer.CompileFixKind.MissingImport => "missing_import",
            CompileErrorAnalyzer.CompileFixKind.WrongImport => "wrong_import",
            CompileErrorAnalyzer.CompileFixKind.PackageMismatch => "package_mismatch",
            CompileErrorAnalyzer.CompileFixKind.MissingBean => "missing_bean",
            CompileErrorAnalyzer.CompileFixKind.MissingField => "missing_field",
            _ => "missing_symbol"
        };

    public static string BuildRepairSignature(IReadOnlyList<ErrorReport> errors) =>
        string.Join(" || ", errors
            .OrderBy(e => e.FilePath ?? string.Empty)
            .ThenBy(e => e.LineNumber ?? 0)
            .Select(e =>
                $"{(e.ErrorType ?? string.Empty).Trim().ToLowerInvariant()}|" +
                $"{(e.FilePath ?? string.Empty).Trim().ToLowerInvariant()}|" +
                $"{NormalizeForSignature(e.Message)}"));

    private static List<ErrorReport> EnrichFromBuildLog(
        IReadOnlyList<ErrorReport> errors,
        string logExcerpt,
        IReadOnlyList<GeneratedFile> files)
    {
        var list = errors.ToList();
        foreach (Match m in MavenErrorLine.Matches(logExcerpt))
        {
            var path = NormalizeRepoPath(m.Groups["path"].Value, files);
            if (string.IsNullOrWhiteSpace(path))
                continue;
            var line = int.TryParse(m.Groups["line"].Value, out var ln) ? ln : (int?)null;
            if (list.Any(e => string.Equals(e.FilePath, path, StringComparison.OrdinalIgnoreCase)))
                continue;
            list.Add(new ErrorReport(
                "CompileError",
                m.Value.Trim(),
                "Fix the compilation error at this location.",
                path,
                line,
                "LogHeuristics"));
        }

        if (logExcerpt.Contains("Duplicated tag: 'build'", StringComparison.OrdinalIgnoreCase)
            && list.All(e => !string.Equals(e.FilePath, "backend/pom.xml", StringComparison.OrdinalIgnoreCase)))
        {
            list.Insert(0, new ErrorReport(
                "ManifestError",
                "Non-parseable POM: duplicated <build> section in backend/pom.xml",
                "Merge into a single <build><plugins> block; do not append a second <build>.",
                "backend/pom.xml",
                diagnosingAgent: "CompileRepairPlanner"));
        }

        return list;
    }

    private static List<List<ErrorReport>> ClusterErrors(IReadOnlyList<ErrorReport> errors)
    {
        var clusters = new List<List<ErrorReport>>();
        foreach (var error in errors)
        {
            var category = ClassifyRootCause(error, string.Empty, null);
            var cluster = clusters.FirstOrDefault(c =>
                c.Count > 0 && ClassifyRootCause(c[0], string.Empty, null) == category
                && (string.IsNullOrWhiteSpace(error.FilePath)
                    || c.Any(x => string.Equals(x.FilePath, error.FilePath, StringComparison.OrdinalIgnoreCase)
                                  || SameDirectory(x.FilePath, error.FilePath))));
            if (cluster is null)
                clusters.Add(new List<ErrorReport> { error });
            else
                cluster.Add(error);
        }

        return clusters;
    }

    private static ErrorReport SelectRootCause(
        IReadOnlyList<ErrorReport> errors,
        string logExcerpt,
        IReadOnlyList<GeneratedFile> files)
    {
        var pom = errors.FirstOrDefault(e =>
            string.Equals(e.FilePath, "backend/pom.xml", StringComparison.OrdinalIgnoreCase)
            || (e.Message?.Contains("POM", StringComparison.OrdinalIgnoreCase) ?? false)
            || (e.Message?.Contains("pom.xml", StringComparison.OrdinalIgnoreCase) ?? false));
        if (pom is not null)
            return pom;

        if (logExcerpt.Contains("Duplicated tag: 'build'", StringComparison.OrdinalIgnoreCase))
        {
            return new ErrorReport(
                "ManifestError",
                "Duplicated <build> in backend/pom.xml",
                "Merge plugins into one <build> section.",
                "backend/pom.xml",
                diagnosingAgent: "CompileRepairPlanner");
        }

        var package = errors.FirstOrDefault(e =>
            e.Message?.Contains("package", StringComparison.OrdinalIgnoreCase) == true
            || e.Message?.Contains("does not exist", StringComparison.OrdinalIgnoreCase) == true);
        if (package is not null)
            return package;

        var symbol = errors.FirstOrDefault(e =>
            e.Message?.Contains("cannot find symbol", StringComparison.OrdinalIgnoreCase) == true);
        if (symbol is not null)
            return symbol;

        return errors[0];
    }

    private static IEnumerable<ErrorReport> SelectRelatedErrors(
        IReadOnlyList<ErrorReport> errors,
        ErrorReport root,
        int max)
    {
        if (string.IsNullOrWhiteSpace(root.FilePath))
            return errors.Where(e => !ReferenceEquals(e, root)).Take(max);

        return errors
            .Where(e => !ReferenceEquals(e, root))
            .Where(e =>
                string.Equals(e.FilePath, root.FilePath, StringComparison.OrdinalIgnoreCase)
                || SameDirectory(e.FilePath, root.FilePath)
                || IsManifestPath(e.FilePath))
            .Take(max);
    }

    private static ErrorReport EnrichError(
        ErrorReport error,
        string logExcerpt,
        string category,
        bool isRoot)
    {
        var sb = new StringBuilder();
        sb.Append(error.Message);
        if (isRoot)
            sb.Append($" [ROOT_CAUSE category={category}]");
        if (isRoot && category is "missing_class" or "missing_interface" or "package_mismatch" or "wrong_import" or "missing_import")
            sb.Append(" [prefer_deterministic_compile_symbol_recovery=true]");
        if (!string.IsNullOrWhiteSpace(error.SuggestedFix))
            sb.Append($" | fix: {error.SuggestedFix}");
        sb.AppendLine();
        sb.AppendLine("Relevant build log excerpt:");
        sb.AppendLine(Truncate(logExcerpt, isRoot ? 3500 : 1200));
        return new ErrorReport(
            error.ErrorType,
            sb.ToString().Trim(),
            error.SuggestedFix,
            error.FilePath,
            error.LineNumber,
            error.DiagnosingAgent ?? "CompileRepairPlanner");
    }

    private static string ClassifyRootCause(
        ErrorReport error,
        string logExcerpt,
        CompileErrorAnalyzer.CompileErrorAnalysis? symbolAnalysis)
    {
        if (symbolAnalysis is not null
            && symbolAnalysis.Kind != CompileErrorAnalyzer.CompileFixKind.Unknown)
            return PlannerCategoryFromAnalysis(symbolAnalysis);

        var signal = $"{error.ErrorType} {error.Message} {error.FilePath} {logExcerpt}";
        if (signal.Contains("pom", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("POM", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("Duplicated tag", StringComparison.OrdinalIgnoreCase))
            return "manifest_pom";
        if (signal.Contains("package", StringComparison.OrdinalIgnoreCase))
            return "package";
        if (signal.Contains("cannot find symbol", StringComparison.OrdinalIgnoreCase))
            return "missing_symbol";
        if (signal.Contains("npm", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("TS", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("frontend", StringComparison.OrdinalIgnoreCase))
            return "frontend";
        return "compile";
    }

    private static string ExtractBuildLogExcerpt(ExecutionResult execution)
    {
        var lines = execution.Logs
            .Select(l => l.Message)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToList();
        if (lines.Count == 0)
            return string.Empty;

        var tail = string.Join('\n', lines.TakeLast(120));
        var errorLines = lines
            .Where(l => l.Contains("[ERROR]", StringComparison.OrdinalIgnoreCase)
                        || l.Contains("error", StringComparison.OrdinalIgnoreCase)
                        || l.Contains("FAILED", StringComparison.OrdinalIgnoreCase)
                        || l.Contains("FATAL", StringComparison.OrdinalIgnoreCase))
            .TakeLast(40)
            .ToList();
        if (errorLines.Count == 0)
            return Truncate(tail, 5000);

        return Truncate(string.Join('\n', errorLines) + "\n---\n" + tail, 6000);
    }

    private static string? NormalizeRepoPath(string raw, IReadOnlyList<GeneratedFile> files)
    {
        var path = raw.Replace('\\', '/');
        if (path.Contains("/backend/", StringComparison.OrdinalIgnoreCase))
        {
            var idx = path.IndexOf("/backend/", StringComparison.OrdinalIgnoreCase);
            path = path[(idx + 1)..];
        }

        if (path.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase))
            return path;

        return files.FirstOrDefault(f => path.EndsWith(f.RelativePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
            ?.RelativePath;
    }

    private static bool SameDirectory(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;
        var da = Path.GetDirectoryName(a.Replace('\\', '/')) ?? string.Empty;
        var db = Path.GetDirectoryName(b.Replace('\\', '/')) ?? string.Empty;
        return string.Equals(da, db, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsManifestPath(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && (path.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

    private static string NormalizeForSignature(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        var s = text.Trim().ToLowerInvariant();
        return s.Length <= 200 ? s : s[..200];
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[^max..];
}
