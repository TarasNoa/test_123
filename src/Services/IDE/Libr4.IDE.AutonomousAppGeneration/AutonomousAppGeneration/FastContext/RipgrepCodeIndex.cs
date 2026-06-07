using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Rust;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FastContext;

public sealed class RipgrepCodeIndex
{
    private const int MaxMatches = 120;
    private readonly ILogger<RipgrepCodeIndex> _logger;

    public RipgrepCodeIndex(ILogger<RipgrepCodeIndex> logger) => _logger = logger;

    public async Task<IReadOnlyList<CodebaseSearchHit>> SearchAsync(
        string workspaceRoot,
        string query,
        CodebaseSearchOptions options,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (RustFastContextBridge.TrySearch(workspaceRoot, query, options, _logger, out var rustHits))
            return rustHits;

        if (IsRipgrepAvailable())
        {
            try
            {
                return await SearchWithRipgrepAsync(workspaceRoot, query, options, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ripgrep search failed, falling back to scan");
            }
        }

        return ScanFallback(workspaceRoot, query, options, ct);
    }

    public async Task<CodebaseIndexManifest> BuildManifestAsync(string workspaceRoot, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (RustFastContextBridge.TryBuildManifest(workspaceRoot, _logger, out var rustManifest)
            && rustManifest is not null)
            return rustManifest;

        var files = new List<CodebaseIndexedFile>();
        foreach (var abs in EnumerateSourceFiles(workspaceRoot))
        {
            ct.ThrowIfCancellationRequested();
            var rel = Path.GetRelativePath(workspaceRoot, abs).Replace('\\', '/');
            var info = new FileInfo(abs);
            var hash = await Sha256OfFileAsync(abs, ct).ConfigureAwait(false);
            files.Add(new CodebaseIndexedFile(rel, hash, info.Length));
        }

        return new CodebaseIndexManifest(
            workspaceRoot,
            HashWorkspace(workspaceRoot),
            DateTime.UtcNow,
            files.Count,
            files);
    }

    private async Task<IReadOnlyList<CodebaseSearchHit>> SearchWithRipgrepAsync(
        string workspaceRoot,
        string query,
        CodebaseSearchOptions options,
        CancellationToken ct)
    {
        var args = new List<string>
        {
            "--json",
            "--line-number",
            "--no-heading",
            "--max-count", MaxMatches.ToString(),
            "--glob", "!**/node_modules/**",
            "--glob", "!**/.git/**",
            "--glob", "!**/dist/**",
            query,
            "."
        };

        var output = await RunProcessAsync("rg", workspaceRoot, args, ct).ConfigureAwait(false);
        var hits = new List<CodebaseSearchHit>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            ct.ThrowIfCancellationRequested();
            using var doc = JsonDocument.Parse(line);
            if (!doc.RootElement.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "match")
                continue;

            var data = doc.RootElement.GetProperty("data");
            var path = data.GetProperty("path").GetProperty("text").GetString() ?? string.Empty;
            var rel = Path.GetRelativePath(workspaceRoot, path).Replace('\\', '/');
            if (!PassesFilters(rel, options))
                continue;

            var lineNumber = data.GetProperty("line_number").GetInt32();
            var text = data.GetProperty("lines").GetProperty("text").GetString() ?? string.Empty;
            hits.Add(new CodebaseSearchHit(
                rel,
                lineNumber,
                lineNumber,
                1.0,
                text.TrimEnd(),
                "ripgrep"));
        }

        return hits;
    }

    private static List<CodebaseSearchHit> ScanFallback(
        string workspaceRoot,
        string query,
        CodebaseSearchOptions options,
        CancellationToken ct)
    {
        var pattern = Regex.Escape(query);
        var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));
        var hits = new List<CodebaseSearchHit>();

        foreach (var abs in EnumerateSourceFiles(workspaceRoot))
        {
            ct.ThrowIfCancellationRequested();
            var rel = Path.GetRelativePath(workspaceRoot, abs).Replace('\\', '/');
            if (!PassesFilters(rel, options))
                continue;

            var lines = File.ReadAllLines(abs);
            for (var i = 0; i < lines.Length; i++)
            {
                if (!regex.IsMatch(lines[i]))
                    continue;

                hits.Add(new CodebaseSearchHit(rel, i + 1, i + 1, 0.8, lines[i].Trim(), "scan"));
                if (hits.Count >= MaxMatches)
                    return hits;
            }
        }

        return hits;
    }

    private static IEnumerable<string> EnumerateSourceFiles(string workspaceRoot)
    {
        if (!Directory.Exists(workspaceRoot))
            yield break;

        foreach (var file in Directory.EnumerateFiles(workspaceRoot, "*", SearchOption.AllDirectories))
        {
            if (ShouldSkip(file))
                continue;
            yield return file;
        }
    }

    private static bool PassesFilters(string relativePath, CodebaseSearchOptions options)
    {
        if (!options.IncludeTests && IsTestPath(relativePath))
            return false;

        if (options.Languages is { Count: > 0 })
        {
            var ext = Path.GetExtension(relativePath).TrimStart('.').ToLowerInvariant();
            if (!options.Languages.Any(l => ext.Equals(l.TrimStart('.'), StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }

    private static bool IsTestPath(string path) =>
        path.Contains("/test/", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("_test.py", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".spec.ts", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".test.ts", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldSkip(string path)
    {
        var p = path.Replace('\\', '/');
        return p.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/.git/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/dist/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/.venv/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRipgrepAvailable()
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "rg",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            p?.WaitForExit(3000);
            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string> RunProcessAsync(
        string fileName,
        string workingDirectory,
        IReadOnlyList<string> args,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"{fileName}_not_available");
        var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return stdout;
    }

    private static async Task<string> Sha256OfFileAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string HashWorkspace(string workspaceRoot) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(workspaceRoot)))).ToLowerInvariant()[..16];
}
