using Libr4.Chat.Application.Abstractions;
using ChatEntity = Libr4.Chat.Domain.Chats.Chat;
using Libr4.Chat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Infrastructure.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly ChatDbContext _dbContext;

    public ChatRepository(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ChatEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Chats
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<List<ChatEntity>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Chats
            .Include(c => c.Participants)
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ChatEntity chat, CancellationToken cancellationToken = default)
    {
        await _dbContext.Chats.AddAsync(chat, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ChatEntity chat, CancellationToken cancellationToken = default)
    {
        _dbContext.Chats.Update(chat);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
