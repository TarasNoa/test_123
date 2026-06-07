using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Patching;

public interface IPatchAttemptRecorder
{
    Task RecordAsync(Guid? runId, string path, string patch, PatchApplyResult result, CancellationToken ct = default);
}

public sealed class PatchAttemptRecorder : IPatchAttemptRecorder
{
    private readonly AgentRuntimeOptions _options;

    public PatchAttemptRecorder(IOptions<AgentRuntimeOptions> options) => _options = options.Value;

    public Task RecordAsync(Guid? runId, string path, string patch, PatchApplyResult result, CancellationToken ct = default)
    {
        if (runId is null)
            return Task.CompletedTask;

        var dir = Path.Combine(_options.RunsRoot, runId.Value.ToString("D"), "patches");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Sanitize(path)}.json");
        var payload = JsonSerializer.Serialize(new
        {
            path,
            success = result.Success,
            mode = result.Mode.ToString(),
            conflict = result.ConflictReport,
            patch,
            timestamp = DateTime.UtcNow
        });
        File.WriteAllText(file, payload);
        return Task.CompletedTask;
    }

    private static string Sanitize(string path) =>
        string.Concat(path.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
