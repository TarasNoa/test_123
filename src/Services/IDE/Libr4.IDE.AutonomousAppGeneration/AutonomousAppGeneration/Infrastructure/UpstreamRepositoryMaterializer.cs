using System.Diagnostics;
using System.Text;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Clones the discovered upstream repository and embeds a bounded snapshot under <c>upstream/</c>
/// so shadow workspaces and quality gates see real source, not probe-only JSON.
/// </summary>
internal static class UpstreamRepositoryMaterializer
{
    private const int MaxFiles = 48;
    private const int MaxBytesPerFile = 48 * 1024;
    private const int MaxTotalBytes = 2 * 1024 * 1024;
    private static readonly string[] SkipDirNames =
    {
        ".git", "node_modules", "bin", "obj", "dist", "build", ".next", "coverage", "vendor"
    };

    public static async Task<UpstreamMaterializeResult> TryMaterializeIntoFilesAsync(
        string bootstrapDetails,
        IList<GeneratedFile> targetFiles,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (!RepoBootstrapDetailsParser.TryParse(bootstrapDetails, out var probe))
            return UpstreamMaterializeResult.Skipped("no_parseable_clone_url");

        var repoName = ExtractRepoName(probe.CloneUrl);
        var tempRoot = Path.Combine(Path.GetTempPath(), "libr4-upstream", Guid.NewGuid().ToString("N"));
        var clonePath = Path.Combine(tempRoot, repoName);

        try
        {
            var cloned = await CloneAsync(probe.CloneUrl, clonePath, logger, ct).ConfigureAwait(false);
            if (!cloned)
                return UpstreamMaterializeResult.Failed(probe.CloneUrl, "git_clone_exit_nonzero");

            var commit = await TryReadHeadCommitAsync(clonePath, ct).ConfigureAwait(false);
            var snapshot = CollectSnapshotFiles(clonePath, probe, commit);
            if (snapshot.Count == 0)
                return UpstreamMaterializeResult.Failed(probe.CloneUrl, "empty_upstream_snapshot");

            var merged = 0;
            foreach (var file in snapshot)
            {
                var idx = -1;
                for (var i = 0; i < targetFiles.Count; i++)
                {
                    if (!targetFiles[i].RelativePath.Equals(file.RelativePath, StringComparison.OrdinalIgnoreCase))
                        continue;
                    idx = i;
                    break;
                }

                if (idx < 0)
                    targetFiles.Add(file);
                else
                    targetFiles[idx] = file;
                merged++;
            }

            logger.LogInformation(
                "Upstream materialized {Repo} -> {Count} file(s), commit={Commit}",
                probe.Repository ?? repoName,
                merged,
                commit ?? "unknown");

            return UpstreamMaterializeResult.Ok(probe.CloneUrl, commit, merged);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Upstream clone/materialize failed for {Url}", probe.CloneUrl);
            return UpstreamMaterializeResult.Failed(probe.CloneUrl, ex.Message);
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    private static async Task<bool> CloneAsync(
        string cloneUrl,
        string targetPath,
        ILogger logger,
        CancellationToken ct)
    {
        if (Directory.Exists(targetPath))
            Directory.Delete(targetPath, recursive: true);

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"clone --depth 1 \"{cloneUrl}\" \"{targetPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null)
            return false;

        var stderr = new StringBuilder();
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                stderr.AppendLine(e.Data);
        };
        process.BeginErrorReadLine();

        using var reg = ct.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // best effort
            }
        });

        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            logger.LogWarning(
                "git clone failed (exit {Code}) for {Url}: {Err}",
                process.ExitCode,
                cloneUrl,
                stderr.ToString().Trim());
            return false;
        }

        return Directory.Exists(targetPath);
    }

    private static async Task<string?> TryReadHeadCommitAsync(string clonePath, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "rev-parse --short HEAD",
            WorkingDirectory = clonePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null)
            return null;

        var output = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return process.ExitCode == 0 ? output.Trim() : null;
    }

    private static List<GeneratedFile> CollectSnapshotFiles(
        string clonePath,
        RepoBootstrapProbe probe,
        string? commit)
    {
        var files = new List<GeneratedFile>();
        var totalBytes = 0;

        void TryAdd(string relativePath, string language, string content)
        {
            if (files.Count >= MaxFiles)
                return;
            var bytes = Encoding.UTF8.GetByteCount(content);
            if (bytes > MaxBytesPerFile || totalBytes + bytes > MaxTotalBytes)
                return;

            files.Add(new GeneratedFile($"upstream/{relativePath}", language, content));
            totalBytes += bytes;
        }

        var manifest = new StringBuilder();
        manifest.AppendLine("{");
        manifest.AppendLine($"  \"clone_url\": \"{EscapeJson(probe.CloneUrl)}\",");
        if (!string.IsNullOrWhiteSpace(probe.RepoUrl))
            manifest.AppendLine($"  \"repo_url\": \"{EscapeJson(probe.RepoUrl)}\",");
        if (!string.IsNullOrWhiteSpace(probe.Repository))
            manifest.AppendLine($"  \"repository\": \"{EscapeJson(probe.Repository)}\",");
        if (!string.IsNullOrWhiteSpace(probe.License))
            manifest.AppendLine($"  \"license\": \"{EscapeJson(probe.License)}\",");
        if (!string.IsNullOrWhiteSpace(commit))
            manifest.AppendLine($"  \"commit\": \"{EscapeJson(commit)}\",");
        manifest.AppendLine("  \"adapted_from_upstream\": true,");
        manifest.AppendLine("  \"snapshot_prefix\": \"upstream/\"");
        manifest.AppendLine("}");
        TryAdd("UPSTREAM_MANIFEST.json", "json", manifest.ToString());

        foreach (var path in Directory.EnumerateFiles(clonePath, "*", SearchOption.AllDirectories))
        {
            if (files.Count >= MaxFiles)
                break;

            var rel = Path.GetRelativePath(clonePath, path).Replace('\\', '/');
            if (ShouldSkipRelativePath(rel))
                continue;

            var ext = Path.GetExtension(path);
            if (!IsInterestingFile(rel, ext))
                continue;

            byte[] bytes;
            try
            {
                var info = new FileInfo(path);
                if (info.Length > MaxBytesPerFile)
                    continue;
                bytes = File.ReadAllBytes(path);
            }
            catch
            {
                continue;
            }

            if (!IsMostlyText(bytes))
                continue;

            var content = Encoding.UTF8.GetString(bytes);
            TryAdd(rel, GuessLanguage(ext), content);
        }

        return files;
    }

    private static bool ShouldSkipRelativePath(string rel)
    {
        var parts = rel.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(p => SkipDirNames.Contains(p, StringComparer.OrdinalIgnoreCase));
    }

    private static bool IsInterestingFile(string rel, string ext)
    {
        if (rel.Equals("UPSTREAM_MANIFEST.json", StringComparison.OrdinalIgnoreCase))
            return false;

        var lower = rel.ToLowerInvariant();
        if (lower is "license" or "license.md" or "license.txt" or "copying" or "readme.md" or "readme")
            return true;

        if (lower.StartsWith("docs/", StringComparison.Ordinal) && ext is ".md" or ".txt")
            return true;

        if (ext is ".md" or ".txt" or ".json" or ".yaml" or ".yml" or ".cs" or ".ts" or ".tsx"
            or ".js" or ".jsx" or ".py" or ".go" or ".rs" or ".fs" or ".fsx")
            return lower.Count(c => c == '/') <= 4;

        return false;
    }

    private static bool IsMostlyText(byte[] bytes)
    {
        if (bytes.Length == 0)
            return true;
        var nonText = 0;
        foreach (var b in bytes)
        {
            if (b is 9 or 10 or 13 or >= 32 and < 127)
                continue;
            nonText++;
            if (nonText * 20 > bytes.Length)
                return false;
        }

        return true;
    }

    private static string GuessLanguage(string ext) =>
        ext.ToLowerInvariant() switch
        {
            ".cs" => "csharp",
            ".ts" or ".tsx" => "typescript",
            ".js" or ".jsx" => "javascript",
            ".py" => "python",
            ".go" => "go",
            ".rs" => "rust",
            ".fs" or ".fsx" => "fsharp",
            ".json" => "json",
            ".md" => "markdown",
            _ => "text"
        };

    private static string ExtractRepoName(string cloneUrl)
    {
        var trimmed = cloneUrl.TrimEnd('/');
        if (trimmed.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^4];
        var last = trimmed.LastIndexOf('/');
        return last >= 0 ? trimmed[(last + 1)..] : "upstream";
    }

    private static string EscapeJson(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best effort
        }
    }
}

internal readonly record struct UpstreamMaterializeResult(
    bool Attempted,
    bool Succeeded,
    string OutcomeCode,
    string? CloneUrl,
    string? Commit,
    int FilesMerged)
{
    public static UpstreamMaterializeResult Skipped(string code) =>
        new(false, false, code, null, null, 0);

    public static UpstreamMaterializeResult Failed(string cloneUrl, string code) =>
        new(true, false, code, cloneUrl, null, 0);

    public static UpstreamMaterializeResult Ok(string cloneUrl, string? commit, int merged) =>
        new(true, true, "upstream_snapshot_materialized", cloneUrl, commit, merged);
}
