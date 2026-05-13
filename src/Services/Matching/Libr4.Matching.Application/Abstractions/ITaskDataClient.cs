namespace Libr4.Matching.Application.Abstractions;

public sealed record TaskData(
    Guid Id,
    string Title,
    string Description,
    string Category,
    decimal Budget,
    DateTimeOffset CreatedAt);

public interface ITaskDataClient
{
    Task<TaskData?> GetTaskAsync(Guid taskId, CancellationToken ct = default);
}
