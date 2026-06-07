using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>Background pre-warm of shared Maven toolchain on host startup.</summary>
public sealed class ShadowToolchainWarmCacheHostedService : BackgroundService
{
    private readonly IShadowToolchainWarmCache _cache;
    private readonly ShadowToolchainWarmCacheOptions _options;
    private readonly ILogger<ShadowToolchainWarmCacheHostedService> _logger;

    public ShadowToolchainWarmCacheHostedService(
        IShadowToolchainWarmCache cache,
        IOptions<ShadowToolchainWarmCacheOptions> options,
        ILogger<ShadowToolchainWarmCacheHostedService> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.PrewarmOnHostStartup || !OperatingSystem.IsWindows())
            return;

        try
        {
            _logger.LogInformation("Shadow warm-cache: pre-warming Maven toolchain in background.");
            await _cache.EnsureMavenToolchainAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // host shutdown
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shadow warm-cache: pre-warm failed; will retry on first workspace.");
        }
    }
}
