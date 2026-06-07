namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Pool-level warm caches reused across shadow workspaces (Maven toolchain, .m2, npm cache).
/// </summary>
public interface IShadowToolchainWarmCache
{
    bool IsEnabled { get; }

    string CacheRoot { get; }

    string MavenLocalRepositoryPath { get; }

    string NpmCachePath { get; }

    bool IsMavenReady { get; }

    /// <summary>Ensures shared Maven binary + local repo directories exist.</summary>
    Task EnsureMavenToolchainAsync(CancellationToken ct = default);

    /// <summary>Links or configures workspace to use shared caches before first build.</summary>
    Task PrepareWorkspaceAsync(string workspaceHostPath, CancellationToken ct = default);

    /// <summary>cmd.exe prefix: JAVA_HOME, Maven PATH, shared local repo.</summary>
    string BuildMavenEnvironmentExports();

    /// <summary>cmd.exe prefix for npm commands using shared cache.</summary>
    string BuildNpmCacheExports();

    /// <summary>Absolute path to mvn.cmd in the shared cache (empty if not ready).</summary>
    string ResolveMavenExecutablePath();

    /// <summary>Injects shared local repo and non-interactive Maven flags into a build command.</summary>
    string EnrichMavenInvocation(string command);
}
