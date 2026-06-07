using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.ExecPolicy;

public interface IObscuraExecPolicyJsonlAudit
{
    Task WriteAsync(ObscuraExecPolicyAuditEntry entry, CancellationToken ct = default);
}

public sealed class ObscuraExecPolicyJsonlAudit : IObscuraExecPolicyJsonlAudit
{
    private readonly AgentRuntimeOptions _options;
    private readonly object _lock = new();

    public ObscuraExecPolicyJsonlAudit(IOptions<AgentRuntimeOptions> options) => _options = options.Value;

    public Task WriteAsync(ObscuraExecPolicyAuditEntry entry, CancellationToken ct = default)
    {
        if (entry.RunId is not Guid runId)
            return Task.CompletedTask;

        var dir = Path.Combine(_options.RunsRoot, runId.ToString("D"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "obscura-exec-audit.jsonl");
        var line = JsonSerializer.Serialize(entry);
        lock (_lock)
            File.AppendAllText(path, line + Environment.NewLine);

        return Task.CompletedTask;
    }
}
