using Libr4.AI.Domain.Agents;

namespace Libr4.AI.Infrastructure.Repositories;

public class AgentRepository : IAgentRepository
{
    private readonly AIDbContext _context;

    public AgentRepository(AIDbContext context)
    {
        _context = context;
    }

    public async Task<Agent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Agents.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<Agent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Agents.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        await _context.Agents.AddAsync(agent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        _context.Agents.Update(agent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var agent = await GetByIdAsync(id, cancellationToken);
        if (agent != null)
        {
            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}