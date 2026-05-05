namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Manager;

public enum ManagedAgentStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public sealed record ManagedAgentTask(
    Guid Id,
    string Name,
    string Objective,
    DateTime CreatedAtUtc,
    ManagedAgentStatus Status);

public interface IManagerSurfaceService
{
    ManagedAgentTask Enqueue(string name, string objective);
    ManagedAgentTask UpdateStatus(Guid id, ManagedAgentStatus status);
    IReadOnlyList<ManagedAgentTask> List();
}
