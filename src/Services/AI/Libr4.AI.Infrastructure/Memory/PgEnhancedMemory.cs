using Libr4.AI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Libr4.AI.Infrastructure.Memory;

public class MemoryRecord
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class PgEnhancedMemory : IEnhancedMemory
{
    private readonly AIDbContext _dbContext;

    public PgEnhancedMemory(AIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddMemoryAsync(string userId, string content, Dictionary<string, string>? metadata = null)
    {
        var record = new MemoryRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = content,
            Metadata = metadata ?? new(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _dbContext.Set<MemoryRecord>().AddAsync(record);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<MemoryItem>> RetrieveAsync(string userId, string query, int topK = 5)
    {
        var records = await _dbContext.Set<MemoryRecord>()
            .Where(m => m.UserId == userId && EF.Functions.Like(m.Content, $"%{query}%"))
            .OrderByDescending(m => m.CreatedAt)
            .Take(topK)
            .ToListAsync();

        return records.Select(r => new MemoryItem
        {
            Id = r.Id.ToString(),
            Content = r.Content,
            Similarity = 0.5f,
            Metadata = r.Metadata
        }).ToList();
    }

    public async Task DeleteMemoryAsync(string userId, string memoryId)
    {
        if (Guid.TryParse(memoryId, out var id))
        {
            var record = await _dbContext.Set<MemoryRecord>()
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
            if (record != null)
            {
                _dbContext.Set<MemoryRecord>().Remove(record);
                await _dbContext.SaveChangesAsync();
            }
        }
    }

    public async Task ClearMemoriesAsync(string userId)
    {
        var records = await _dbContext.Set<MemoryRecord>()
            .Where(m => m.UserId == userId)
            .ToListAsync();
        _dbContext.Set<MemoryRecord>().RemoveRange(records);
        await _dbContext.SaveChangesAsync();
    }
}
