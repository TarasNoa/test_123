using System.Collections.Concurrent;

namespace Libr4.IDE.Infrastructure.Containers;

/// <summary>
/// Pre-warmed container pool for instant Shadow Workspace startup
/// Maintains 5-10 warm containers ready for immediate use
/// Startup time: < 2 seconds (vs 10-15 seconds for cold start)
/// </summary>
public interface IPreWarmedContainerPool
{
    /// <summary>
    /// Get a warm container immediately, or null if pool exhausted
    /// </summary>
    Task<WarmContainer?> AcquireAsync(string workspaceId, CancellationToken ct = default);
    
    /// <summary>
    /// Return container to pool for reuse
    /// </summary>
    Task ReleaseAsync(string containerId, CancellationToken ct = default);
    
    /// <summary>
    /// Current pool statistics
    /// </summary>
    PoolStats GetStats();
    
    /// <summary>
    /// Warm up N containers immediately
    /// </summary>
    Task WarmUpAsync(int count, CancellationToken ct = default);
    
    /// <summary>
    /// Stop all warm containers
    /// </summary>
    Task ShutdownAsync(CancellationToken ct = default);
}

/// <summary>
/// Implementation of pre-warmed container pool
/// </summary>
public class PreWarmedContainerPool : IPreWarmedContainerPool, IDisposable
{
    private readonly ConcurrentBag<WarmContainer> _warmContainers = new();
    private readonly ConcurrentDictionary<string, WarmContainer> _inUse = new();
    private readonly IContainerManager _containerManager;
    private readonly ILogger<PreWarmedContainerPool> _logger;
    private readonly Timer _maintenanceTimer;
    private readonly string _baseImage;
    
    private const int MinPoolSize = 5;
    private const int MaxPoolSize = 10;
    private const int MaintenanceIntervalMs = 30000; // 30 seconds

    public PreWarmedContainerPool(
        IContainerManager containerManager,
        ILogger<PreWarmedContainerPool> logger,
        IConfiguration configuration)
    {
        _containerManager = containerManager;
        _logger = logger;
        _baseImage = configuration["ShadowWorkspace:BaseImage"] ?? "node:18-alpine";
        
        // Start maintenance timer
        _maintenanceTimer = new Timer(
            async _ => await MaintainPoolAsync(),
            null,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(MaintenanceIntervalMs));
        
        _logger.LogInformation("PreWarmedContainerPool initialized with base image {Image}", _baseImage);
    }

    public async Task<WarmContainer?> AcquireAsync(string workspaceId, CancellationToken ct = default)
    {
        _logger.LogInformation("Acquiring warm container for workspace {WorkspaceId}", workspaceId);
        
        // Try to get from warm pool
        if (_warmContainers.TryTake(out var warmContainer))
        {
            _logger.LogInformation(
                "Warm container acquired: {ContainerId} for {WorkspaceId} (Pool: {Remaining} remaining)",
                warmContainer.ContainerId, workspaceId, _warmContainers.Count);
            
            // Attach workspace volume
            await AttachWorkspaceAsync(warmContainer.ContainerId, workspaceId, ct);
            
            // Mark as in-use
            _inUse[workspaceId] = warmContainer;
            warmContainer.AssignedToWorkspaceId = workspaceId;
            warmContainer.AssignedAt = DateTime.UtcNow;
            
            // Trigger background warm-up to replenish pool
            _ = Task.Run(() => MaintainPoolAsync());
            
            return warmContainer;
        }
        
        _logger.LogWarning("No warm containers available in pool for {WorkspaceId}", workspaceId);
        return null;
    }

    public async Task ReleaseAsync(string containerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Releasing container {ContainerId} back to pool", containerId);
        
        // Find and remove from in-use
        var entry = _inUse.FirstOrDefault(x => x.Value.ContainerId == containerId);
        if (entry.Key != null)
        {
            _inUse.TryRemove(entry.Key, out _);
        }
        
        // Reset container state
        try
        {
            await ResetContainerAsync(containerId, ct);
            
            // Return to warm pool if healthy
            var warmContainer = new WarmContainer
            {
                ContainerId = containerId,
                BaseImage = _baseImage,
                WarmedAt = DateTime.UtcNow,
                IsHealthy = true
            };
            
            if (_warmContainers.Count < MaxPoolSize)
            {
                _warmContainers.Add(warmContainer);
                _logger.LogInformation("Container {ContainerId} returned to warm pool", containerId);
            }
            else
            {
                // Pool full, destroy container
                await _containerManager.RemoveContainerAsync(containerId);
                _logger.LogInformation("Container {ContainerId} destroyed (pool full)", containerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset container {ContainerId}, destroying", containerId);
            await _containerManager.RemoveContainerAsync(containerId);
        }
    }

    public PoolStats GetStats()
    {
        return new PoolStats
        {
            WarmContainersAvailable = _warmContainers.Count,
            InUseContainers = _inUse.Count,
            TotalPoolSize = _warmContainers.Count + _inUse.Count,
            MinTargetSize = MinPoolSize,
            MaxTargetSize = MaxPoolSize
        };
    }

    public async Task WarmUpAsync(int count, CancellationToken ct = default)
    {
        _logger.LogInformation("Warming up {Count} containers", count);
        
        var tasks = new List<Task>();
        for (int i = 0; i < count; i++)
        {
            if (_warmContainers.Count >= MaxPoolSize)
            {
                _logger.LogInformation("Pool at max capacity ({Max}), stopping warm-up", MaxPoolSize);
                break;
            }
            
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var containerId = await CreateWarmContainerAsync(ct);
                    if (!string.IsNullOrEmpty(containerId))
                    {
                        var warmContainer = new WarmContainer
                        {
                            ContainerId = containerId,
                            BaseImage = _baseImage,
                            WarmedAt = DateTime.UtcNow,
                            IsHealthy = true
                        };
                        _warmContainers.Add(warmContainer);
                        _logger.LogDebug("Container {ContainerId} warmed and added to pool", containerId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to warm up container");
                }
            }, ct));
        }
        
        await Task.WhenAll(tasks);
        _logger.LogInformation("Warm-up complete. Pool size: {Size}", _warmContainers.Count);
    }

    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Shutting down pre-warmed container pool");
        
        // Stop maintenance timer
        await _maintenanceTimer.DisposeAsync();
        
        // Clean up all warm containers
        var allContainers = _warmContainers.ToList();
        _warmContainers.Clear();
        
        foreach (var container in allContainers)
        {
            try
            {
                await _containerManager.RemoveContainerAsync(container.ContainerId);
                _logger.LogDebug("Destroyed warm container {ContainerId}", container.ContainerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to destroy warm container {ContainerId}", container.ContainerId);
            }
        }
        
        // Clean up in-use containers
        foreach (var entry in _inUse)
        {
            try
            {
                await _containerManager.RemoveContainerAsync(entry.Value.ContainerId);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to destroy in-use container {ContainerId} during shutdown", entry.Value.ContainerId);
            }
        }
        _inUse.Clear();
        
        _logger.LogInformation("Pool shutdown complete");
    }

    private async Task MaintainPoolAsync()
    {
        try
        {
            var stats = GetStats();
            _logger.LogDebug(
                "Pool maintenance: {Warm} warm, {InUse} in-use (target: {Min}-{Max})",
                stats.WarmContainersAvailable, stats.InUseContainers,
                MinPoolSize, MaxPoolSize);
            
            // Replenish if below minimum
            if (stats.WarmContainersAvailable < MinPoolSize)
            {
                var needed = MinPoolSize - stats.WarmContainersAvailable;
                _logger.LogInformation("Pool below minimum, warming {Needed} containers", needed);
                await WarmUpAsync(needed);
            }
            
            // Clean up stale containers (> 10 minutes old)
            var staleThreshold = DateTime.UtcNow.AddMinutes(-10);
            var staleContainers = _warmContainers
                .Where(c => c.WarmedAt < staleThreshold)
                .ToList();
            
            foreach (var stale in staleContainers)
            {
                if (_warmContainers.TryTake(out _))
                {
                    try
                    {
                        await _containerManager.RemoveContainerAsync(stale.ContainerId);
                        _logger.LogDebug("Removed stale container {ContainerId}", stale.ContainerId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to remove stale container {ContainerId}", stale.ContainerId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pool maintenance");
        }
    }

    private async Task<string> CreateWarmContainerAsync(CancellationToken ct)
    {
        // Create container with base image, no workspace yet
        var tempId = $"warm-{Guid.NewGuid():N}";
        var container = await _containerManager.CreateContainerAsync(tempId, _baseImage);
        
        // Pre-install common dependencies
        await PreinstallDependenciesAsync(container.Id, ct);
        
        return container.Id;
    }

    private async Task PreinstallDependenciesAsync(string containerId, CancellationToken ct)
    {
        // Pre-install node_modules for common frameworks
        var installScript = @"
            cd /tmp && 
            mkdir -p /workspace/cache && 
            cd /workspace/cache &&
            npm install --legacy-peer-deps react react-dom next typescript @types/react @types/node 2>/dev/null || true &&
            npm install --legacy-peer-deps express cors dotenv 2>/dev/null || true &&
            echo 'Dependencies cached'
        ";
        
        try
        {
            await _containerManager.ExecuteCommandAsync(containerId, installScript);
            _logger.LogDebug("Pre-installed dependencies in {ContainerId}", containerId);
        }
        catch
        {
            // Non-critical, container still usable
            _logger.LogWarning("Failed to pre-install dependencies in {ContainerId}", containerId);
        }
    }

    private async Task AttachWorkspaceAsync(string containerId, string workspaceId, CancellationToken ct)
    {
        // Mount workspace volume
        var mountScript = $@"
            mkdir -p /workspace/{workspaceId} &&
            ln -sf /workspace/{workspaceId} /workspace/current
        ";
        
        await _containerManager.ExecuteCommandAsync(containerId, mountScript);
        _logger.LogDebug("Attached workspace {WorkspaceId} to {ContainerId}", workspaceId, containerId);
    }

    private async Task ResetContainerAsync(string containerId, CancellationToken ct)
    {
        // Clean up workspace, keep cached dependencies
        var resetScript = @"
            rm -rf /workspace/current/* /workspace/current/.* 2>/dev/null || true &&
            pkill -f node || true &&
            echo 'Container reset'
        ";
        
        await _containerManager.ExecuteCommandAsync(containerId, resetScript);
        _logger.LogDebug("Reset container {ContainerId}", containerId);
    }

    public void Dispose()
    {
        _maintenanceTimer?.Dispose();
        ShutdownAsync().Wait(TimeSpan.FromSeconds(30));
    }
}

/// <summary>
/// Warm container metadata
/// </summary>
public class WarmContainer
{
    public string ContainerId { get; set; } = string.Empty;
    public string BaseImage { get; set; } = string.Empty;
    public DateTime WarmedAt { get; set; }
    public bool IsHealthy { get; set; }
    public string? AssignedToWorkspaceId { get; set; }
    public DateTime? AssignedAt { get; set; }
}

/// <summary>
/// Pool statistics
/// </summary>
public class PoolStats
{
    public int WarmContainersAvailable { get; set; }
    public int InUseContainers { get; set; }
    public int TotalPoolSize { get; set; }
    public int MinTargetSize { get; set; }
    public int MaxTargetSize { get; set; }
}
