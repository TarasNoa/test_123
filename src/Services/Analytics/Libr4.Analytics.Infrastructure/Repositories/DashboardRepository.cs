using Microsoft.EntityFrameworkCore;
using Libr4.Analytics.Application.Abstractions;
using Libr4.Analytics.Domain.Dashboards;

namespace Libr4.Analytics.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AnalyticsDbContext _context;

    public DashboardRepository(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<Dashboard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Dashboards.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<Dashboard>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Dashboards
            .AsNoTracking()
            .Where(d => d.OwnerId == ownerId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Dashboard dashboard, CancellationToken cancellationToken = default)
    {
        await _context.Dashboards.AddAsync(dashboard, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Dashboard dashboard, CancellationToken cancellationToken = default)
    {
        _context.Dashboards.Update(dashboard);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
