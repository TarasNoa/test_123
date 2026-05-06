using Libr4.IDE.Domain.FSharp;
using Libr4.IDE.Infrastructure.Persistence;
using Libr4.IDE.Infrastructure.Clients;

namespace Libr4.IDE.Application.Orchestration;

public class SandboxOrchestrator 
{
    private readonly ApplicationDbContext _db;
    private readonly IGrpcSandboxClient _rust;

    public SandboxOrchestrator(ApplicationDbContext db, IGrpcSandboxClient rust)
    {
        _db = db;
        _rust = rust;
    }

    public async Task RunTaskSecurely(Guid agentId, string code, CancellationToken ct = default)
    {
        var taskId = Guid.NewGuid();
        
        // 1. Atomic state update via F#
        var agent = await _db.Agents.FindAsync([agentId], ct);
        if (agent == null) return;

        agent.State = StatePersistence.deserializeState(
            StateMachine.transition(
                StatePersistence.serializeState(agent.State), 
                StatePersistence.serializeEvent(AgentEvent.NewTaskAssigned(taskId))
            )
        );
        await _db.SaveChangesAsync(ct);

        try 
        {
            // 2. Call Rust with result waiting and cancellation support
            var result = await _rust.ExecuteCodeAsync(new ExecutionRequest
            {
                TaskId = taskId.ToString(),
                Code = code,
                Language = "python"
            }, ct);

            // 3. Handle Rust result (Termination Reason)
            AgentEvent finalEvent = result.TerminationReason switch
            {
                "Success" => AgentEvent.NewExecutionCompleted(result.Stdout ?? ""),
                "MemoryLimit" => AgentEvent.NewCriticalError("Rust: OOM Killer triggered"),
                "Timeout" => AgentEvent.NewCriticalError("Rust: Watchdog timeout"),
                _ => AgentEvent.NewCriticalError($"Rust: {result.Stderr ?? "Unknown error"}")
            };

            // 4. Final state transition
            agent.State = StatePersistence.deserializeState(
                StateMachine.transition(
                    StatePersistence.serializeState(agent.State), 
                    StatePersistence.serializeEvent(finalEvent)
                )
            );
            await _db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation
            agent.State = StatePersistence.deserializeState(
                StateMachine.transition(
                    StatePersistence.serializeState(agent.State), 
                    StatePersistence.serializeEvent(AgentEvent.NewCancel)
                )
            );
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            agent.State = StatePersistence.deserializeState(
                StateMachine.transition(
                    StatePersistence.serializeState(agent.State), 
                    StatePersistence.serializeEvent(AgentEvent.NewCriticalError(ex.Message))
                )
            );
            await _db.SaveChangesAsync(ct);
        }
    }
}
