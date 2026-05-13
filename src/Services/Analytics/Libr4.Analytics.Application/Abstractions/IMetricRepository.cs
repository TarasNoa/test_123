using Libr4.Analytics.Domain.Metrics;

namespace Libr4.Analytics.Application.Abstractions;

public interface IMetricRepository
{
    Task<Metric?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Metric>> GetMetricsAsync(string? name = null, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);
    Task AddAsync(Metric metric, CancellationToken cancellationToken = default);
    Task UpdateAsync(Metric metric, CancellationToken cancellationToken = default);
}
