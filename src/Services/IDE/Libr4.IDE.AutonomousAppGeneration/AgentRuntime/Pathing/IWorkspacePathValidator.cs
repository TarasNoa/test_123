namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Pathing;

public interface IWorkspacePathValidator
{
    PathValidationResult Validate(string relativePath, ToolContextPaths context);
    void AuditDenied(PathValidationResult result, string toolName, Guid? runId);
}

public sealed record ToolContextPaths(string WorkspaceRoot, Guid? RunId);

public sealed record PathValidationResult(bool Allowed, string NormalizedPath, string? DenyReason);

public interface IPathAccessAudit
{
    void RecordDenied(string toolName, string path, string reason, Guid? runId);
    IReadOnlyList<PathAccessDeniedEntry> GetDenied(Guid? runId);
}

public sealed record PathAccessDeniedEntry(
    string ToolName,
    string Path,
    string Reason,
    Guid? RunId,
    DateTime TimestampUtc);

public sealed class InMemoryPathAccessAudit : IPathAccessAudit
{
    private readonly List<PathAccessDeniedEntry> _entries = new();
    private readonly object _lock = new();

    public void RecordDenied(string toolName, string path, string reason, Guid? runId)
    {
        lock (_lock)
        {
            _entries.Add(new PathAccessDeniedEntry(toolName, path, reason, runId, DateTime.UtcNow));
        }
    }

    public IReadOnlyList<PathAccessDeniedEntry> GetDenied(Guid? runId) =>
        _entries.Where(e => e.RunId == runId).ToList();
}
