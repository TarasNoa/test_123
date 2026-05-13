using Libr4.Analytics.Application.Abstractions;
using Libr4.Analytics.Domain.Metrics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Libr4.Analytics.Application;

public class MetricsService : IMetricsService
{
    private readonly IMetricRepository _metricRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(IMetricRepository metricRepository, IDistributedCache cache, ILogger<MetricsService> logger)
    {
        _metricRepository = metricRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<MetricDto>> GetMetricsAsync(string name = null, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"metrics:{name ?? "all"}:{from}:{to}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            _logger.LogInformation("Retrieved metrics from cache");
            return JsonSerializer.Deserialize<List<MetricDto>>(cached)!;
        }

        var metrics = await _metricRepository.GetMetricsAsync(name, from, to, cancellationToken);
        var result = metrics.Select(m => new MetricDto(m.Id, m.Name, m.Type, m.Value, m.Timestamp, m.Labels)).ToList();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, cancellationToken);

        return result;
    }

    public async Task<MetricDto> CreateMetricAsync(CreateMetricRequest request, CancellationToken cancellationToken = default)
    {
        var metric = Metric.Create(request.Name, request.Type, request.Value, request.Labels);
        await _metricRepository.AddAsync(metric, cancellationToken);

        // Invalidate cache
        await _cache.RemoveAsync("metrics:all:*", cancellationToken);

        return new MetricDto(metric.Id, metric.Name, metric.Type, metric.Value, metric.Timestamp, metric.Labels);
    }

    public async Task UpdateMetricAsync(Guid id, double value, CancellationToken cancellationToken = default)
    {
        var metric = await _metricRepository.GetByIdAsync(id, cancellationToken);
        if (metric == null) throw new InvalidOperationException("Metric not found");

        metric.UpdateValue(value);
        await _metricRepository.UpdateAsync(metric, cancellationToken);

        // Invalidate cache
        await _cache.RemoveAsync($"metrics:{metric.Name}:*", cancellationToken);
    }
}