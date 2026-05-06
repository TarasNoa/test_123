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

        // Проверка владения агентом
        var agent = await _db.Agents.FindAsync(Guid.Parse(agentId));
        if (agent == null || agent.OwnerId != userId)
        {
            throw new HubException("Forbidden: You don't own this agent");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, agentId);

        // Начальная синхронизация: отправляем текущее состояние сразу
        await Clients.Caller.SendAsync("OnAgentStateUpdated", new
        {
            AgentId = agent.Id,
            State = agent.State,
            Timestamp = DateTime.UtcNow,
            IsInitialSync = true
        });
    }

    public async Task UnsubscribeFromAgent(string agentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, agentId);
    }
}
