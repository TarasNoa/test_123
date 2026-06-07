using System.Diagnostics;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Persists Maven/npm caches under <c>%TEMP%/libr4-shadow-pool/_warm-cache</c> so new workspaces
/// do not re-download toolchains or dependencies on every run.
/// </summary>
public sealed class ShadowToolchainWarmCache : IShadowToolchainWarmCache
{
    private readonly ShadowToolchainWarmCacheOptions _options;
    private readonly ILogger<ShadowToolchainWarmCache> _logger;
    private readonly SemaphoreSlim _mavenGate = new(1, 1);
    private volatile bool _mavenReady;

    public ShadowToolchainWarmCache(
        IOptions<ShadowToolchainWarmCacheOptions> options,
        ILogger<ShadowToolchainWarmCache> logger)
    {
        _options = options.Value;
        _logger = logger;
        CacheRoot = Path.Combine(Path.GetTempPath(), "libr4-shadow-pool", "_warm-cache");
        MavenLocalRepositoryPath = Path.Combine(CacheRoot, "m2", "repository");
        NpmCachePath = Path.Combine(CacheRoot, "npm-cache");
    }

    public bool IsEnabled => _options.Enabled;

    public string CacheRoot { get; }

    public string MavenLocalRepositoryPath { get; }

    public string NpmCachePath { get; }

    public bool IsMavenReady => _mavenReady || File.Exists(ResolveMavenExecutablePath());

    public async Task EnsureMavenToolchainAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled || !OperatingSystem.IsWindows())
            return;

        if (IsMavenReady)
        {
            _mavenReady = true;
            return;
        }

        await _mavenGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (File.Exists(ResolveMavenExecutablePath()))
            {
                _mavenReady = true;
                return;
            }

            Directory.CreateDirectory(CacheRoot);
            Directory.CreateDirectory(MavenLocalRepositoryPath);
            Directory.CreateDirectory(NpmCachePath);

            var mavenHome = Path.Combine(CacheRoot, "apache-maven");
            var mvnCmd = Path.Combine(mavenHome, "bin", "mvn.cmd");
            if (File.Exists(mvnCmd))
            {
                _mavenReady = true;
                _logger.LogInformation("Shadow warm-cache: Maven already present at {Path}", mvnCmd);
                return;
            }

            _logger.LogInformation("Shadow warm-cache: bootstrapping portable Maven into {Root}", CacheRoot);
            var zipPath = Path.Combine(CacheRoot, "maven.zip");
            var script = $$"""
                $ErrorActionPreference = 'Stop'
                $zip = '{{zipPath.Replace("'", "''")}}'
                $uri = '{{JavaReactWindowsToolchainBootstrap.MavenDownloadUri}}'
                $dest = '{{CacheRoot.Replace("'", "''")}}'
                if (-not (Test-Path $zip)) { Invoke-WebRequest -Uri $uri -OutFile $zip }
                Expand-Archive -Force $zip $dest
                if (Test-Path (Join-Path $dest 'apache-maven-3.9.9')) {
                  if (Test-Path (Join-Path $dest 'apache-maven')) { Remove-Item -Recurse -Force (Join-Path $dest 'apache-maven') }
                  Move-Item -Force (Join-Path $dest 'apache-maven-3.9.9') (Join-Path $dest 'apache-maven')
                }
                """;

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)
                                  ?? throw new InvalidOperationException("Failed to start PowerShell for Maven bootstrap.");
            var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            if (process.ExitCode != 0 || !File.Exists(mvnCmd))
            {
                _logger.LogWarning(
                    "Shadow warm-cache: Maven bootstrap failed (exit={Exit}). stdout={Stdout} stderr={Stderr}",
                    process.ExitCode,
                    TrimForLog(stdout),
                    TrimForLog(stderr));
                return;
            }

            _mavenReady = true;
            _logger.LogInformation("Shadow warm-cache: Maven ready at {Path}", mvnCmd);
        }
        finally
        {
            _mavenGate.Release();
        }
    }

    public async Task PrepareWorkspaceAsync(string workspaceHostPath, CancellationToken ct = default)
    {
        if (!_options.Enabled || !OperatingSystem.IsWindows())
            return;

        await EnsureMavenToolchainAsync(ct).ConfigureAwait(false);
        Directory.CreateDirectory(NpmCachePath);

        if (!IsMavenReady)
            return;

        var sharedMaven = Path.Combine(CacheRoot, "apache-maven");
        var workspaceToolchain = Path.Combine(workspaceHostPath, ".libr4-toolchain");
        var workspaceMaven = Path.Combine(workspaceToolchain, "apache-maven");
        Directory.CreateDirectory(workspaceToolchain);

        if (Directory.Exists(workspaceMaven))
        {
            var linkTarget = TryReadJunctionTarget(workspaceMaven);
            if (string.Equals(linkTarget, sharedMaven, StringComparison.OrdinalIgnoreCase))
                return;
        }
        else if (File.Exists(workspaceMaven))
        {
            return;
        }

        TryDeletePath(workspaceMaven);
        if (TryCreateJunction(workspaceMaven, sharedMaven))
        {
            _logger.LogDebug(
                "Shadow warm-cache: linked workspace Maven junction {Workspace} -> {Shared}",
                workspaceMaven,
                sharedMaven);
        }
    }

    public string BuildMavenEnvironmentExports()
    {
        if (!_options.Enabled)
            return JavaReactWindowsToolchainBootstrap.MavenPathExports;

        var m2 = MavenLocalRepositoryPath.Replace("/", "\\");
        if (!IsMavenReady)
        {
            return $"{JavaReactWindowsToolchainBootstrap.MavenPathExports} && " +
                   $"set \"MAVEN_OPTS=-Dmaven.repo.local={m2}\"";
        }

        var mvn = ResolveMavenExecutablePath().Replace("/", "\\");
        return $"{JavaReactWindowsToolchainBootstrap.JavaHomeExports} && " +
               $"set \"PATH={Path.GetDirectoryName(mvn)};%PATH%\" && " +
               $"set \"MAVEN_OPTS=-Dmaven.repo.local={m2}\"";
    }

    public string BuildNpmCacheExports()
    {
        if (!_options.Enabled)
            return string.Empty;

        var cache = NpmCachePath.Replace("/", "\\");
        return $"set \"npm_config_cache={cache}\" && set \"NPM_CONFIG_CACHE={cache}\"";
    }

    public string ResolveMavenExecutablePath() =>
        Path.Combine(CacheRoot, "apache-maven", "bin", "mvn.cmd");

    public string EnrichMavenInvocation(string command)
    {
        if (string.IsNullOrWhiteSpace(command) || !_options.Enabled)
            return command;

        var enriched = command;
        if (enriched.Contains(" mvn -q", StringComparison.OrdinalIgnoreCase)
            || enriched.Contains(" mvn.cmd -q", StringComparison.OrdinalIgnoreCase))
        {
            enriched = enriched
                .Replace(" mvn -q", " mvn -B -ntp", StringComparison.OrdinalIgnoreCase)
                .Replace(" mvn.cmd -q", " mvn.cmd -B -ntp", StringComparison.OrdinalIgnoreCase);
        }

        if (!enriched.Contains("maven.repo.local", StringComparison.OrdinalIgnoreCase))
        {
            var m2 = MavenLocalRepositoryPath.Replace("/", "\\");
            var match = System.Text.RegularExpressions.Regex.Match(
                enriched,
                @"""[^""]*mvn\.cmd""|\bmvn(?:\.cmd)?\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var insertAt = match.Index + match.Length;
                enriched = enriched.Insert(insertAt, $" -Dmaven.repo.local=\"{m2}\"");
            }
        }

        return enriched;
    }

    private static bool TryCreateJunction(string linkPath, string targetPath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is null)
                return false;
            process.WaitForExit();
            return process.ExitCode == 0 && Directory.Exists(linkPath);
        }
        catch
        {
            return false;
        }
    }

    private static string? TryReadJunctionTarget(string junctionPath)
    {
        try
        {
            var item = new DirectoryInfo(junctionPath);
            if (item.Exists && item.Attributes.HasFlag(FileAttributes.ReparsePoint))
                return item.ResolveLinkTarget(true)?.FullName;
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static void TryDeletePath(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path);
            else if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignore — mklink may still succeed
        }
    }

    private static string TrimForLog(string value) =>
        value.Length <= 500 ? value : value[..500] + "...";
}
