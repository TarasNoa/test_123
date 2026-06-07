using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

public sealed class GitUpstreamCloneProvider : IUpstreamCloneProvider
{
    private readonly ILogger<GitUpstreamCloneProvider> _logger;

    public GitUpstreamCloneProvider(ILogger<GitUpstreamCloneProvider> logger) => _logger = logger;

    public async Task<UpstreamCloneHandle?> TryShallowCloneAsync(string cloneUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cloneUrl))
            return null;

        var repoName = ExtractRepoName(cloneUrl);
        var tempRoot = Path.Combine(Path.GetTempPath(), "libr4-cascade-clone", Guid.NewGuid().ToString("N"));
        var clonePath = Path.Combine(tempRoot, repoName);

        try
        {
            Directory.CreateDirectory(tempRoot);
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"clone --depth 1 \"{cloneUrl}\" \"{clonePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start git clone process.");
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            if (process.ExitCode != 0 || !Directory.Exists(clonePath))
            {
                _logger.LogDebug("Cascade upstream clone failed for {Url} (exit={ExitCode})", cloneUrl, process.ExitCode);
                TryDeleteDirectory(tempRoot);
                return null;
            }

            return new UpstreamCloneHandle(clonePath, cloneUrl);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Cascade upstream clone failed for {Url}", cloneUrl);
            TryDeleteDirectory(tempRoot);
            return null;
        }
    }

    private static string ExtractRepoName(string cloneUrl)
    {
        var trimmed = cloneUrl.Trim().TrimEnd('/');
        if (trimmed.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^4];
        var last = trimmed.LastIndexOf('/');
        return last >= 0 ? trimmed[(last + 1)..] : "upstream";
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }
}
