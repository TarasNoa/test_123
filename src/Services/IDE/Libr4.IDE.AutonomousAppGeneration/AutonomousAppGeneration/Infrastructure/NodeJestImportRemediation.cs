using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic fixes for Jest/Vitest "Cannot find module" failures on relative imports in tests.
/// </summary>
public static class NodeJestImportRemediation
{
    private static readonly Regex CannotFindModule = new(
        @"Cannot find module '([^']+)'",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static int Apply(IList<GeneratedFile> files, GenerationPlan plan, string? buildLog)
    {
        if (string.IsNullOrWhiteSpace(buildLog))
            return 0;

        if (!buildLog.Contains("Cannot find module", StringComparison.OrdinalIgnoreCase)
            && !buildLog.Contains("Module not found", StringComparison.OrdinalIgnoreCase))
            return 0;

        var changed = 0;
        foreach (Match match in CannotFindModule.Matches(buildLog))
        {
            var spec = match.Groups[1].Value.Trim();
            if (!spec.StartsWith(".", StringComparison.Ordinal))
                continue;

            var testFile = InferReporterFile(buildLog) ?? files
                .FirstOrDefault(f => IsTestFile(f.RelativePath) && (f.Content?.Contains(spec, StringComparison.Ordinal) ?? false))
                ?.RelativePath;
            if (string.IsNullOrWhiteSpace(testFile))
                continue;

            var fileList = files as IReadOnlyList<GeneratedFile> ?? files.ToList();
            var analysis = CompileErrorAnalyzer.Analyze(
                new ErrorReport("MissingImport", match.Value, suggestedFix: string.Empty, filePath: testFile),
                buildLog,
                fileList,
                plan);
            changed += NodeTsCompileSymbolRemediation.Apply(files, plan, analysis);
        }

        return changed > 0 ? 1 : 0;
    }

    private static string? InferReporterFile(string buildLog)
    {
        var line = buildLog.Split('\n')
            .FirstOrDefault(l => l.Contains("Cannot find module", StringComparison.OrdinalIgnoreCase)
                                 && (l.Contains(".test.", StringComparison.OrdinalIgnoreCase)
                                     || l.Contains(".spec.", StringComparison.OrdinalIgnoreCase)
                                     || l.Contains("/tests/", StringComparison.OrdinalIgnoreCase)));
        if (line is null)
            return null;

        var match = Regex.Match(line, @"([\w./\\-]+\.(?:test|spec)\.(?:ts|tsx|js|jsx))", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Replace('\\', '/') : null;
    }

    private static bool IsTestFile(string path) =>
        path.Contains(".test.", StringComparison.OrdinalIgnoreCase)
        || path.Contains(".spec.", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
        || path.Contains("\\tests\\", StringComparison.OrdinalIgnoreCase);
}
