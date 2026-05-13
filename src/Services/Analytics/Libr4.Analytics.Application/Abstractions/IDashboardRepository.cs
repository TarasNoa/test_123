using Libr4.Analytics.Domain.Dashboards;

namespace Libr4.Analytics.Application.Abstractions;

public interface IDashboardRepository
{
    Task<Dashboard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Dashboard>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task AddAsync(Dashboard dashboard, CancellationToken cancellationToken = default);
    Task UpdateAsync(Dashboard dashboard, CancellationToken cancellationToken = default);
}
