using System.Runtime.CompilerServices;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Api;

public sealed class AgentRuntimeStreamService
{
    private readonly AgentRuntimeOptions _options;
    private readonly IRolloutRecorder _rollout;

    public AgentRuntimeStreamService(IOptions<AgentRuntimeOptions> options, IRolloutRecorder rollout)
    {
        _options = options.Value;
        _rollout = rollout;
    }

    public async IAsyncEnumerable<string> StreamEventsAsync(
        Guid runId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var path = Path.Combine(_options.RunsRoot, runId.ToString("D"), "events.jsonl");
        long offset = 0;
        while (!ct.IsCancellationRequested)
        {
            if (File.Exists(path))
            {
                await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream.Seek(offset, SeekOrigin.Begin);
                using var reader = new StreamReader(stream);
                string? line;
                while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) is not null)
                {
                    offset = stream.Position;
                    if (!string.IsNullOrWhiteSpace(line))
                        yield return line;
                }
            }

            await Task.Delay(500, ct).ConfigureAwait(false);
        }
    }

    public Task<IReadOnlyList<RolloutEntry>> GetRolloutAsync(Guid runId, CancellationToken ct = default) =>
        _rollout.GetRolloutAsync(runId, ct);

    public Task<IReadOnlyList<RolloutSearchHit>> SearchRolloutAsync(string query, int limit, CancellationToken ct = default) =>
        _rollout.SearchAsync(query, limit, ct);
}
