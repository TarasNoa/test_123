using System.Text;
using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;

public static class CiRepairLogParser
{
    private static readonly Regex FileLineRegex = new(
        @"(?<path>[A-Za-z0-9_./\\-]+)\((?<line>\d+)(?:,\d+)?\):\s*(?<msg>.+)",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));

    private static readonly Regex PathColonLineRegex = new(
        @"(?<path>[A-Za-z0-9_./\\-]+\.[A-Za-z0-9]+):(?<line>\d+)(?::\d+)?:\s*(?<msg>.+)",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));

    private static readonly string[] ErrorMarkers =
    [
        "error ",
        "error:",
        "failed",
        "failure",
        "npm err",
        "pytest",
        "assertionerror",
        "exception",
        "cannot find",
        "undefined",
        "exit code"
    ];

    public static CiRepairLogParseResult Parse(string? rawLog, int maxExcerptChars = 8000, int maxLines = 120)
    {
        if (string.IsNullOrWhiteSpace(rawLog))
            return new CiRepairLogParseResult(string.Empty, Array.Empty<string>(), Array.Empty<ErrorReport>());

        var lines = rawLog.Replace("\r\n", "\n").Split('\n');
        var errorLines = lines
            .Where(IsErrorLine)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxLines)
            .ToList();

        var excerpt = BuildExcerpt(errorLines, maxExcerptChars);
        var errors = new List<ErrorReport>();
        var focusPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in errorLines)
        {
            if (TryParseFileReference(line, out var path, out var lineNo, out var message))
            {
                focusPaths.Add(NormalizePath(path));
                errors.Add(new ErrorReport(
                    "ci_failure",
                    message,
                    "Fix CI failure indicated by GitHub Actions log.",
                    NormalizePath(path),
                    lineNo,
                    diagnosingAgent: "ci_log_parser"));
            }
            else
            {
                errors.Add(new ErrorReport(
                    "ci_failure",
                    line.Trim(),
                    "Fix CI failure indicated by GitHub Actions log.",
                    diagnosingAgent: "ci_log_parser"));
            }
        }

        return new CiRepairLogParseResult(excerpt, focusPaths.ToList(), errors);
    }

    public static long? TryParseRunIdFromLogsUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var match = Regex.Match(url, @"/actions/runs/(\d+)", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        return match.Success && long.TryParse(match.Groups[1].Value, out var runId) ? runId : null;
    }

    public static string BuildRepairTask(
        IReadOnlyList<string> focusPaths,
        string logExcerpt,
        string? prefetchText)
    {
        var sb = new StringBuilder();
        sb.AppendLine("GitHub Actions CI failed after PR ship. Fix the failures with minimal surgical changes.");
        if (focusPaths.Count > 0)
        {
            sb.AppendLine("Prioritize these paths from CI logs:");
            foreach (var path in focusPaths)
                sb.AppendLine($"- {path}");
        }

        if (!string.IsNullOrWhiteSpace(logExcerpt))
        {
            sb.AppendLine();
            sb.AppendLine("CI log excerpt:");
            sb.AppendLine(logExcerpt);
        }

        if (!string.IsNullOrWhiteSpace(prefetchText))
        {
            sb.AppendLine();
            sb.AppendLine("Prefetched codebase context:");
            sb.AppendLine(prefetchText);
        }

        sb.AppendLine("After fixes, ensure build/test commands from the plan would pass in CI.");
        return sb.ToString();
    }

    private static string BuildExcerpt(IReadOnlyList<string> errorLines, int maxChars)
    {
        if (errorLines.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var line in errorLines)
        {
            if (sb.Length + line.Length + 1 > maxChars)
                break;
            sb.AppendLine(line);
        }

        return sb.ToString().Trim();
    }

    private static bool IsErrorLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var lower = line.ToLowerInvariant();
        return ErrorMarkers.Any(marker => lower.Contains(marker, StringComparison.Ordinal));
    }

    private static bool TryParseFileReference(
        string line,
        out string path,
        out int? lineNo,
        out string message)
    {
        path = string.Empty;
        lineNo = null;
        message = line.Trim();

        var match = FileLineRegex.Match(line);
        if (!match.Success)
            match = PathColonLineRegex.Match(line);
        if (!match.Success)
            return false;

        path = match.Groups["path"].Value;
        if (match.Groups["line"].Success && int.TryParse(match.Groups["line"].Value, out var parsedLine))
            lineNo = parsedLine;
        message = match.Groups["msg"].Value.Trim();
        return !string.IsNullOrWhiteSpace(path);
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');
}

public sealed record CiRepairLogParseResult(
    string Excerpt,
    IReadOnlyList<string> FocusPaths,
    IReadOnlyList<ErrorReport> Errors);
