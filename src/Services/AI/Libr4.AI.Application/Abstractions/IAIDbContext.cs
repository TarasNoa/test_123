using Libr4.AI.Domain.Agents;
using Libr4.AI.Domain.Chats;
using Microsoft.EntityFrameworkCore;

namespace Libr4.AI.Application.Abstractions;

public interface IAIDbContext
{
    DbSet<AIChat> Chats { get; }
    DbSet<AIMessage> Messages { get; }
    DbSet<Agent> Agents { get; }
    DbSet<AgentTool> AgentTools { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
