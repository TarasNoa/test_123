using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Model;

namespace Libr4.Gateway;

/// <summary>
/// Dynamic router for Shadow Workspace preview URLs
/// Creates YARP routes at runtime for customer previews
/// </summary>
public class DynamicPreviewRouter : IProxyConfigProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, PreviewRoute> _previewRoutes = new();
    private readonly InMemoryConfig _config;
    private readonly CancellationTokenSource _cts = new();
    private readonly ILogger<DynamicPreviewRouter> _logger;

    public DynamicPreviewRouter(ILogger<DynamicPreviewRouter> logger)
    {
        _logger = logger;
        _config = new InMemoryConfig(new List<RouteConfig>(), new List<ClusterConfig>());
    }

    /// <summary>
    /// Register a new preview route for a shadow workspace
    /// </summary>
    public async Task<string> RegisterPreviewRouteAsync(
        string orderId,
        string customerId,
        string containerEndpoint,
        int containerPort = 3000)
    {
        var routeId = $"preview-{orderId}";
        var pathPrefix = $"/preview/{HashCustomerId(customerId)}/{orderId}";

        var route = new PreviewRoute
        {
            OrderId = orderId,
            CustomerId = customerId,
            RouteId = routeId,
            PathPrefix = pathPrefix,
            ContainerEndpoint = containerEndpoint,
            ContainerPort = containerPort,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };

        // Create YARP route config
        var routeConfig = new RouteConfig
        {
            RouteId = routeId,
            Match = new RouteMatch
            {
                Path = $"{pathPrefix}/{{**catchall}}"
            },
            ClusterId = routeId,
            Transforms = new List<Dictionary<string, string>>
            {
                new() { { "PathRemovePrefix", pathPrefix } }
            }
        };

        // Create cluster config
        var clusterConfig = new ClusterConfig
        {
            ClusterId = routeId,
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["default"] = new()
                {
                    Address = $"http://{containerEndpoint}:{containerPort}"
                }
            },
            HealthCheck = new HealthCheckConfig
            {
                Active = new ActiveHealthCheckConfig
                {
                    Enabled = true,
                    Interval = TimeSpan.FromSeconds(30),
                    Timeout = TimeSpan.FromSeconds(10),
                    Policy = "ConsecutiveFailures",
                    Path = "/health"
                }
            }
        };

        _previewRoutes[routeId] = route;

        // Update in-memory config
        var routes = _config.Routes.ToList();
        var clusters = _config.Clusters.ToList();

        // Remove existing route if any
        routes.RemoveAll(r => r.RouteId == routeId);
        clusters.RemoveAll(c => c.ClusterId == routeId);

        routes.Add(routeConfig);
        clusters.Add(clusterConfig);

        _config.Update(routes, clusters);

        _logger.LogInformation(
            "Registered preview route {RouteId} for order {OrderId} at {PathPrefix} -> {Endpoint}",
            routeId, orderId, pathPrefix, containerEndpoint);

        return pathPrefix;
    }

    /// <summary>
    /// Unregister a preview route
    /// </summary>
    public async Task UnregisterPreviewRouteAsync(string orderId)
    {
        var routeId = $"preview-{orderId}";

        if (_previewRoutes.TryRemove(routeId, out var route))
        {
            var routes = _config.Routes.ToList();
            var clusters = _config.Clusters.ToList();

            routes.RemoveAll(r => r.RouteId == routeId);
            clusters.RemoveAll(c => c.ClusterId == routeId);

            _config.Update(routes, clusters);

            _logger.LogInformation("Unregistered preview route {RouteId}", routeId);
        }
    }

    /// <summary>
    /// Get preview route info
    /// </summary>
    public PreviewRoute? GetPreviewRoute(string orderId)
    {
        var routeId = $"preview-{orderId}";
        _previewRoutes.TryGetValue(routeId, out var route);
        return route;
    }

    /// <summary>
    /// List all active preview routes
    /// </summary>
    public IReadOnlyCollection<PreviewRoute> GetActivePreviews()
    {
        return _previewRoutes.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Clean up expired preview routes
    /// </summary>
    public async Task CleanupExpiredRoutesAsync()
    {
        var expired = _previewRoutes
            .Where(r => r.Value.ExpiresAt < DateTime.UtcNow)
            .Select(r => r.Key)
            .ToList();

        foreach (var routeId in expired)
        {
            await UnregisterPreviewRouteAsync(routeId.Replace("preview-", ""));
        }

        if (expired.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired preview routes", expired.Count);
        }
    }

    public IProxyConfig GetConfig() => _config;

    private string HashCustomerId(string customerId)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(customerId));
        return Convert.ToHexString(bytes)[..16].ToLower();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    /// <summary>
    /// In-memory proxy configuration that can be updated at runtime
    /// </summary>
    private class InMemoryConfig : IProxyConfig
    {
        private readonly List<RouteConfig> _routes;
        private readonly List<ClusterConfig> _clusters;
        private readonly CancellationChangeToken _changeToken;

        public InMemoryConfig(List<RouteConfig> routes, List<ClusterConfig> clusters)
        {
            _routes = routes;
            _clusters = clusters;
            _changeToken = new CancellationChangeToken(new CancellationTokenSource().Token);
        }

        public IReadOnlyList<RouteConfig> Routes => _routes.AsReadOnly();
        public IReadOnlyList<ClusterConfig> Clusters => _clusters.AsReadOnly();
        public IChangeToken ChangeToken => _changeToken;

        public void Update(List<RouteConfig> routes, List<ClusterConfig> clusters)
        {
            _routes.Clear();
            _routes.AddRange(routes);
            _clusters.Clear();
            _clusters.AddRange(clusters);
        }
    }
}

/// <summary>
/// Preview route information
/// </summary>
public class PreviewRoute
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string RouteId { get; set; } = string.Empty;
    public string PathPrefix { get; set; } = string.Empty;
    public string ContainerEndpoint { get; set; } = string.Empty;
    public int ContainerPort { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
