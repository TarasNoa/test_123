using Microsoft.EntityFrameworkCore;
using Libr4.Analytics.Application.Abstractions;
using Libr4.Analytics.Domain.Metrics;

namespace Libr4.Analytics.Infrastructure.Repositories;

public class MetricRepository : IMetricRepository
{
    private readonly AnalyticsDbContext _context;

    public MetricRepository(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<Metric?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Metrics.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<Metric>> GetMetricsAsync(string? name = null, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Metrics.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(m => m.Name == name);

        if (from.HasValue)
            query = query.Where(m => m.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(m => m.Timestamp <= to.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Metric metric, CancellationToken cancellationToken = default)
    {
        await _context.Metrics.AddAsync(metric, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Metric metric, CancellationToken cancellationToken = default)
    {
        _context.Metrics.Update(metric);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
