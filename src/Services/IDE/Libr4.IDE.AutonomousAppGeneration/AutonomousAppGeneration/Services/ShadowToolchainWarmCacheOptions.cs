namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Shared toolchain caches under libr4-shadow-pool/_warm-cache (Maven binary, .m2, npm).
/// </summary>
public sealed class ShadowToolchainWarmCacheOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Download portable Maven into the shared cache on host startup (background).</summary>
    public bool PrewarmOnHostStartup { get; set; } = true;
}
