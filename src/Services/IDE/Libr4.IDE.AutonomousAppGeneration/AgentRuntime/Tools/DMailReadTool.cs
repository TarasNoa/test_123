using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.DMail;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class DMailReadTool : IAgentTool
{
    private readonly IDMailBus _bus;

    public DMailReadTool(IDMailBus bus) => _bus = bus;

    public string Name => "dmail_read";
    public string Description => "Read DMail messages. Input: { \"to\": \"frontend\", \"from\": \"backend\", \"unackedOnly\": true }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var runId = context.Session.RunId;
        if (runId is null)
            return Fail("run id unavailable");

        var to = input.TryGetProperty("to", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
        var from = input.TryGetProperty("from", out var f) && f.ValueKind == JsonValueKind.String ? f.GetString() : null;
        var unackedOnly = input.TryGetProperty("unackedOnly", out var u) && u.ValueKind == JsonValueKind.True;

        var messages = await _bus.ReadAsync(runId.Value, to, from, unackedOnly, ct).ConfigureAwait(false);
        if (messages.Count == 0)
            return new ToolExecutionResult(Name, true, "(no messages)", Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());

        var lines = messages.Select(m =>
            $"{m.Id} {m.From}->{m.To} ack={(m.AckedAtUtc is null ? "pending" : "acked")}: {m.Payload}");
        return new ToolExecutionResult(Name, true, string.Join('\n', lines), Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
    }

    private static ToolExecutionResult Fail(string msg) =>
        new("dmail_read", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
