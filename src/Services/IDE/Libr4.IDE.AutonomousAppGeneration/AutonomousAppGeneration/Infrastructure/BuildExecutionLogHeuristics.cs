using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic extraction of <see cref="ErrorReport"/> entries from raw build/test logs
/// when LLM error analysis is unavailable or returns nothing actionable.
/// </summary>
public static class BuildExecutionLogHeuristics
{
    private static readonly Regex MavenJavaError = new(
        @"\[ERROR\]\s+(?<path>[^\s:]+\.java):\[(?<line>\d+)[,\d]*\]\s+(?<msg>[^\r\n]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DotNetError = new(
        @"(?<path>[^\s(]+\.cs)\((?<line>\d+)[,\d]*\):\s+error\s+(?<code>CS\d+):\s+(?<msg>[^\r\n]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NpmTsError = new(
        @"(?<path>[^\s:]+\.(?:tsx?|jsx?)):(?<line>\d+)[:\d]*\s*-\s+error\s+(?<msg>[^\r\n]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IReadOnlyList<ErrorReport> ExtractErrors(
        ExecutionResult execution,
        IReadOnlyList<GeneratedFile> files)
    {
        var blob = string.Join('\n', execution.Logs.Select(l => l.Message));
        if (string.IsNullOrWhiteSpace(blob))
            blob = string.Join('\n', execution.ErrorLogs.Select(l => l.Message));

        if (string.IsNullOrWhiteSpace(blob))
        {
            return new[]
            {
                new ErrorReport(
                    "BuildOrRuntimeError",
                    $"Execution failed with exit code {execution.ExitCode}",
                    "Inspect build logs and fix the first compilation error in the reported file.",
                    diagnosingAgent: "LogHeuristics")
            };
        }

        var list = new List<ErrorReport>();
        foreach (Match m in MavenJavaError.Matches(blob))
        {
            var path = NormalizeRepoRelativePath(m.Groups["path"].Value, files);
            var line = int.TryParse(m.Groups["line"].Value, out var ln) ? ln : (int?)null;
            var msg = m.Groups["msg"].Value.Trim();
            var isTest = path!.Contains("/src/test/", StringComparison.OrdinalIgnoreCase);
            list.Add(new ErrorReport(
                isTest ? "TestCompileError" : "CompileError",
                msg,
                isTest
                    ? "Fix the test or remove it if it references types that do not exist in main sources."
                    : "Fix the Java compilation error in this file (imports, types, or method signatures).",
                path,
                line,
                "LogHeuristics"));
        }

        foreach (Match m in DotNetError.Matches(blob))
        {
            var path = NormalizeRepoRelativePath(m.Groups["path"].Value, files);
            var line = int.TryParse(m.Groups["line"].Value, out var ln) ? ln : (int?)null;
            var code = m.Groups["code"].Value;
            var msg = m.Groups["msg"].Value.Trim();
            list.Add(new ErrorReport(
                "CompileError",
                $"{code}: {msg}",
                "Update the file or project references so the type/namespace resolves.",
                path,
                line,
                "LogHeuristics"));
        }

        foreach (Match m in NpmTsError.Matches(blob))
        {
            var path = NormalizeRepoRelativePath(m.Groups["path"].Value, files);
            var line = int.TryParse(m.Groups["line"].Value, out var ln) ? ln : (int?)null;
            list.Add(new ErrorReport(
                "CompileError",
                m.Groups["msg"].Value.Trim(),
                "Fix the TypeScript/React compile error (imports, types, or component props).",
                path,
                line,
                "LogHeuristics"));
        }

        if (list.Count > 0)
            return Deduplicate(list);

        if (blob.Contains("cannot find symbol", StringComparison.OrdinalIgnoreCase)
            || blob.Contains("package does not exist", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                new ErrorReport(
                    "CompileError",
                    "Java compilation failed (missing symbol or package).",
                    "Align package declarations with directory layout and add missing model/service types.",
                    files.FirstOrDefault(f => f.RelativePath.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase))?.RelativePath,
                    diagnosingAgent: "LogHeuristics")
            };
        }

        if (blob.Contains("BUILD FAILURE", StringComparison.OrdinalIgnoreCase)
            || blob.Contains("Failed to execute goal", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                new ErrorReport(
                    "BuildOrRuntimeError",
                    "Maven build failed.",
                    "Fix the first compilation error in backend Java sources or remove broken generated tests.",
                    files.FirstOrDefault(f => f.RelativePath.Equals("backend/pom.xml", StringComparison.OrdinalIgnoreCase))?.RelativePath,
                    diagnosingAgent: "LogHeuristics")
            };
        }

        var snippet = blob.Length > 600 ? blob.Substring(blob.Length - 600) : blob;
        return new[]
        {
            new ErrorReport(
                "BuildOrRuntimeError",
                snippet.Trim(),
                "Apply minimal fixes for the root cause shown in the build log.",
                diagnosingAgent: "LogHeuristics")
        };
    }

    private static string? NormalizeRepoRelativePath(string raw, IReadOnlyList<GeneratedFile> files)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var path = raw.Replace('\\', '/').Trim();
        var idx = path.IndexOf("/backend/", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            path = path.Substring(idx + 1);

        idx = path.IndexOf("/frontend/", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            path = path.Substring(idx + 1);

        if (path.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase))
            return path;

        var match = files.FirstOrDefault(f =>
            path.EndsWith(f.RelativePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));
        return match?.RelativePath ?? path;
    }

    private static IReadOnlyList<ErrorReport> Deduplicate(List<ErrorReport> list) =>
        list.GroupBy(
                e => $"{e.ErrorType}|{e.FilePath}|{e.LineNumber}|{e.Message}",
                StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(24)
            .ToList();
}
