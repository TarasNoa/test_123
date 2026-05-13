using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Domain.Calls;
using Libr4.Chat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Infrastructure.Repositories;

public class CallRepository : ICallRepository
{
    private readonly ChatDbContext _dbContext;

    public CallRepository(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Call?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Calls.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<Call?> GetActiveByChatIdAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Calls
            .Where(c => c.ChatId == chatId && c.Status != CallStatus.Ended)
            .OrderByDescending(c => c.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Call call, CancellationToken cancellationToken = default)
    {
        await _dbContext.Calls.AddAsync(call, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Call call, CancellationToken cancellationToken = default)
    {
        _dbContext.Calls.Update(call);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
