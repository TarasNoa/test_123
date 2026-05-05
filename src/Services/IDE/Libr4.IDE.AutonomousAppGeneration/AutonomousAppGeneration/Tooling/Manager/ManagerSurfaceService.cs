namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Manager;

public sealed class ManagerSurfaceService : IManagerSurfaceService
{
    private readonly Dictionary<Guid, ManagedAgentTask> _tasks = new();

    public ManagedAgentTask Enqueue(string name, string objective)
    {
        var task = new ManagedAgentTask(
            Id: Guid.NewGuid(),
            Name: string.IsNullOrWhiteSpace(name) ? "agent-task" : name.Trim(),
            Objective: objective ?? string.Empty,
            CreatedAtUtc: DateTime.UtcNow,
            Status: ManagedAgentStatus.Pending);
        _tasks[task.Id] = task;
        return task;
    }

    public ManagedAgentTask UpdateStatus(Guid id, ManagedAgentStatus status)
    {
        if (!_tasks.TryGetValue(id, out var task))
            throw new KeyNotFoundException($"Task {id} is not registered.");

        var updated = task with { Status = status };
        _tasks[id] = updated;
        return updated;
    }

    public IReadOnlyList<ManagedAgentTask> List() => _tasks.Values.OrderByDescending(x => x.CreatedAtUtc).ToArray();
}
