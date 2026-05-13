using Libr4.IDE.Domain.AI;
using Microsoft.EntityFrameworkCore;

namespace Libr4.IDE.Infrastructure.Persistence;

public class EfAIConversationRepository : IAIConversationRepository
{
    private readonly ApplicationDbContext _context;

    public EfAIConversationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<AIConversation?> GetByIdAsync(Guid id)
        => _context.AIConversations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public Task<AIConversation?> GetByIdWithMessagesAsync(Guid id)
        => _context.AIConversations.AsNoTracking().Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == id);

    public Task<List<AIConversation>> GetByUserIdAsync(Guid userId, int skip = 0, int limit = 20, bool archivedOnly = false)
        => _context.AIConversations.AsNoTracking()
            .Where(c => c.UserId == userId && c.IsArchived == archivedOnly)
            .OrderByDescending(c => c.LastMessageAt)
            .Skip(skip).Take(limit)
            .ToListAsync();

    public Task<List<AIMessage>> GetMessagesByConversationIdAsync(Guid conversationId)
        => _context.AIMessages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(AIConversation conversation)
    {
        await _context.AIConversations.AddAsync(conversation);
        await _context.SaveChangesAsync();
    }

    public Task UpdateAsync(AIConversation conversation)
    {
        _context.AIConversations.Update(conversation);
        return _context.SaveChangesAsync();
    }

    public Task DeleteAsync(AIConversation conversation)
    {
        _context.AIConversations.Remove(conversation);
        return _context.SaveChangesAsync();
    }

    public Task<bool> ExistsAsync(Guid id)
        => _context.AIConversations.AnyAsync(c => c.Id == id);
}
