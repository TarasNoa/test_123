using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;

public interface IShadowWorkspaceAccessor
{
    bool TryGetWorkspace(Guid workspaceId, out ShadowWorkspaceContext context);
    Task<ExecResult> ExecAsync(Guid workspaceId, string command, CancellationToken ct = default);
    Task<string> ReadFileAsync(Guid workspaceId, string relativePath, CancellationToken ct = default);
    Task WriteFileAsync(Guid workspaceId, string relativePath, string content, CancellationToken ct = default);
    IReadOnlyList<string> GlobFiles(Guid workspaceId, string globPattern);
}
