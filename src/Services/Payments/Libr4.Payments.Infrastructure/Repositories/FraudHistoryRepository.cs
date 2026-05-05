using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Domain.Invoices;
using Libr4.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Payments.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of fraud history repository.
/// </summary>
public class FraudHistoryRepository : IFraudHistoryRepository
{
    private readonly PaymentsDbContext _dbContext;

    public FraudHistoryRepository(PaymentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> GetFraudCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.FraudHistories
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId, ct);
    }

    public async Task RecordFraudAsync(Guid userId, string reason, CancellationToken ct = default)
    {
        var record = FraudHistory.Create(userId, reason);
        _dbContext.FraudHistories.Add(record);
        await _dbContext.SaveChangesAsync(ct);
    }
}
