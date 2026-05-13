using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Domain.Messages;
using Libr4.Chat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly ChatDbContext _dbContext;

    public MessageRepository(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Messages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public Task<List<Message>> GetByChatIdAsync(Guid chatId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return _dbContext.Messages
            .Where(m => m.ChatId == chatId && !m.IsDeleted)
            .OrderByDescending(m => m.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _dbContext.Messages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Message message, CancellationToken cancellationToken = default)
    {
        _dbContext.Messages.Update(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
