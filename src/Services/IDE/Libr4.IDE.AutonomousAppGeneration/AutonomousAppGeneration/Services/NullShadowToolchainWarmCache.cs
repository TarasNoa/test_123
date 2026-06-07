namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>Disabled warm-cache for tests and lightweight hosts.</summary>
public sealed class NullShadowToolchainWarmCache : IShadowToolchainWarmCache
{
    public bool IsEnabled => false;

    public string CacheRoot => string.Empty;

    public string MavenLocalRepositoryPath => string.Empty;

    public string NpmCachePath => string.Empty;

    public bool IsMavenReady => false;

    public Task EnsureMavenToolchainAsync(CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task PrepareWorkspaceAsync(string workspaceHostPath, CancellationToken ct = default) =>
        Task.CompletedTask;

    public string BuildMavenEnvironmentExports() =>
        Infrastructure.JavaReactWindowsToolchainBootstrap.MavenPathExports;

    public string BuildNpmCacheExports() => string.Empty;

    public string ResolveMavenExecutablePath() => string.Empty;

    public string EnrichMavenInvocation(string command) => command;
}
