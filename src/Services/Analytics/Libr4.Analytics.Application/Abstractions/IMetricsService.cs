using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Libr4.Analytics.Application.Abstractions;

public record MetricDto(
    Guid Id,
    string Name,
    string Type,
    double Value,
    DateTimeOffset Timestamp,
    Dictionary<string, string> Labels);

public record CreateMetricRequest(
    string Name,
    string Type,
    double Value,
    Dictionary<string, string> Labels);

public interface IMetricsService
{
    Task<List<MetricDto>> GetMetricsAsync(string name = null, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);
    Task<MetricDto> CreateMetricAsync(CreateMetricRequest request, CancellationToken cancellationToken = default);
    Task UpdateMetricAsync(Guid id, double value, CancellationToken cancellationToken = default);
}