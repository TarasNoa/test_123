using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.DMail;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class DMailSendTool : IAgentTool
{
    private readonly IDMailBus _bus;

    public DMailSendTool(IDMailBus bus) => _bus = bus;

    public string Name => "dmail_send";
    public string Description => "Send async DMail to another subagent. Input: { \"to\": \"frontend\", \"payload\": \"...\", \"ackRequired\": true }";
    public bool IsReadOnly => false;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var runId = context.Session.RunId;
        if (runId is null)
            return Fail("run id unavailable");

        var to = input.TryGetProperty("to", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
        var payload = input.TryGetProperty("payload", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
        if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(payload))
            return Fail("to and payload required");

        var ackRequired = input.TryGetProperty("ackRequired", out var a) && a.ValueKind == JsonValueKind.True;
        var from = input.TryGetProperty("from", out var f) && f.ValueKind == JsonValueKind.String
            ? f.GetString() ?? "agent"
            : "agent";

        var message = await _bus.SendAsync(runId.Value, from, to!, payload!, ackRequired, ct).ConfigureAwait(false);
        return new ToolExecutionResult(Name, true, $"dmail_sent:{message.Id}", Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
    }

    private static ToolExecutionResult Fail(string msg) =>
        new("dmail_send", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
