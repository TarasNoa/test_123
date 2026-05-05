using Libr4.AI.Application.Abstractions;
using Libr4.AI.Domain.Agents;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.AI.Application.Agents.Queries;

public record GetAgentsQuery(AgentType? Type = null, bool ActiveOnly = true) : IRequest<Result<List<AgentDto>>>;

public record AgentDto(
    Guid Id,
    string Name,
    string Description,
    AgentType Type,
    string Model,
    AgentStatus Status,
    bool IsActive,
    DateTime CreatedAt,
    List<AgentToolDto> Tools);

public record AgentToolDto(
    Guid Id,
    string Name,
    string Description);

public class GetAgentsHandler : IRequestHandler<GetAgentsQuery, Result<List<AgentDto>>>
{
    private readonly IAIDbContext _context;

    public GetAgentsHandler(IAIDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AgentDto>>> Handle(GetAgentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Agents
            .AsNoTracking()
            .Include(a => a.AllowedTools)
            .AsQueryable();

        if (request.Type.HasValue)
            query = query.Where(a => a.Type == request.Type.Value);

        if (request.ActiveOnly)
            query = query.Where(a => a.IsActive);

        var agents = await query
            .OrderBy(a => a.Name)
            .Select(a => new AgentDto(
                a.Id,
                a.Name,
                a.Description,
                a.Type,
                a.Model,
                a.Status,
                a.IsActive,
                a.CreatedAt,
                a.AllowedTools.Select(t => new AgentToolDto(t.Id, t.Name, t.Description)).ToList()))
            .ToListAsync(cancellationToken);

        return Result.Success(agents);
    }
}
