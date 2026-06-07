namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

public interface IDelegationWorkerHost
{
    Task<DelegationWorkerResult> ExecuteAsync(
        DelegationWorkerRequest request,
        Func<CancellationToken, Task<string>> worker,
        CancellationToken ct = default);
}
