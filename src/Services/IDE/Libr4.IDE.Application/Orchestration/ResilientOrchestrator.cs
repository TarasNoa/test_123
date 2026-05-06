using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Libr4.IDE.Domain.FSharp;
using Libr4.IDE.Infrastructure.Persistence;
using Libr4.IDE.Infrastructure.Clients;
using Libr4.IDE.Api.Hubs;

namespace Libr4.IDE.Application.Orchestration;

public class ResilientOrchestrator
{
    private readonly ApplicationDbContext _db;
    private readonly ISandboxClient _rust;
    private readonly IHubContext<AgentHub> _hub;

    public ResilientOrchestrator(ApplicationDbContext db, ISandboxClient rust, IHubContext<AgentHub> hub)
    {
        _db = db;
        _rust = rust;
        _hub = hub;
    }

    public async Task RunSecurelyAsync(Guid agentId, string code, CancellationToken ct)
    {
        // 1. Use transaction for guarantee of recording intention
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        
        var agent = await _db.Agents.FirstOrDefaultAsync(a => a.Id == agentId, ct);
        if (agent == null) return;

        var taskId = Guid.NewGuid();
        agent.State = StatePersistence.deserializeState(
            StateMachine.transition(
                StatePersistence.serializeState(agent.State), 
                StatePersistence.serializeEvent(AgentEvent.NewTaskAssigned(taskId))
            )
        );
        
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        // 2. Execute in Rust with try-catch to record final state even on network failure
        ExecutionResult result;
        try 
        {
            result = await _rust.RunAsync(taskId.ToString(), code, ct);
        }
        catch (Exception ex)
        {
            await HandleFinalState(agentId, AgentEvent.NewCriticalError($"Network/Infrastructure failure: {ex.Message}"), ct);
            return;
        }

        // 3. Map constants instead of "magic strings"
        AgentEvent finalEvent = result.TerminationReason.Trim() switch
        {
            "Success" => AgentEvent.NewExecutionCompleted(result.Stdout ?? ""),
            "MemoryLimit" => AgentEvent.NewCriticalError("OOM: Sandbox killed the process"),
            "Timeout" => AgentEvent.NewCriticalError("Timeout: Execution limit reached"),
            _ => AgentEvent.NewCriticalError($"Runtime Error: {result.Stderr ?? "Unknown error"}")
        };

        await HandleFinalState(agentId, finalEvent, ct);
    }

    private async Task HandleFinalState(Guid agentId, AgentEvent ev, CancellationToken ct)
    {
        // Re-capture context for update
        var agent = await _db.Agents.FindAsync([agentId], ct);
        if (agent != null)
        {
            agent.State = StatePersistence.deserializeState(
                StateMachine.transition(
                    StatePersistence.serializeState(agent.State),
                    StatePersistence.serializeEvent(ev)
                )
            );
            await _db.SaveChangesAsync(ct);

            // Send real-time notification via SignalR
            await _hub.Clients.Group(agentId.ToString()).SendAsync("OnAgentStateUpdated", new
            {
                AgentId = agentId,
                State = StatePersistence.serializeState(agent.State),
                Timestamp = DateTime.UtcNow
            }, ct);
        }
    }
}
