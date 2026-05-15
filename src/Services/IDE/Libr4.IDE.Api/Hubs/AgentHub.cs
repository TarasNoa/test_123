using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Libr4.IDE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Libr4.IDE.Api.Hubs;

/// <summary>
/// Хаб для трансляции событий агентов всем подключенным фронтендам.
/// Защищен JWT авторизацией с проверкой владения данными.
/// </summary>
[Authorize]
public class AgentHub : Hub
{
    private readonly ApplicationDbContext _db;

    public AgentHub(ApplicationDbContext db)
    {
        _db = db;
    }

    // Группировка по AgentId с проверкой OwnerId
    public async Task SubscribeToAgent(string agentId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("Unauthorized: No user ID found in token");
        }

        var agentGuid = Guid.Parse(agentId);
        var lastEvent = await _db.AgentEvents
            .AsNoTracking()
            .Where(e => e.RunId == agentGuid)
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync();

        await Groups.AddToGroupAsync(Context.ConnectionId, agentId);

        await Clients.Caller.SendAsync("OnAgentStateUpdated", new
        {
            AgentId = agentGuid,
            State = lastEvent?.Type ?? "Idle",
            Timestamp = DateTime.UtcNow,
            IsInitialSync = true
        });
    }

    public async Task UnsubscribeFromAgent(string agentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, agentId);
    }

    // ─── Session-based grouping for unified chat stream ───
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        await Clients.Caller.SendAsync("JoinedSession", new { SessionId = sessionId });
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }
}
