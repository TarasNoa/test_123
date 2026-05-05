namespace Libr4.Gateway;

/// <summary>
/// Background service to periodically clean up expired preview routes
/// </summary>
public class PreviewCleanupBackgroundService : BackgroundService
{
    private readonly DynamicPreviewRouter _previewRouter;
    private readonly ILogger<PreviewCleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    public PreviewCleanupBackgroundService(
        DynamicPreviewRouter previewRouter,
        ILogger<PreviewCleanupBackgroundService> logger)
    {
        _previewRouter = previewRouter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Preview cleanup background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _previewRouter.CleanupExpiredRoutesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during preview route cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Preview cleanup background service stopped");
    }
}
