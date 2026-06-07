using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.DMail;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class DMailAckTool : IAgentTool
{
    private readonly IDMailBus _bus;

    public DMailAckTool(IDMailBus bus) => _bus = bus;

    public string Name => "dmail_ack";
    public string Description => "Acknowledge DMail message. Input: { \"messageId\": \"abc123\" }";
    public bool IsReadOnly => false;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var runId = context.Session.RunId;
        if (runId is null)
            return Fail("run id unavailable");

        var messageId = input.TryGetProperty("messageId", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
        if (string.IsNullOrWhiteSpace(messageId))
            return Fail("messageId required");

        var ok = await _bus.AckAsync(runId.Value, messageId!, ct).ConfigureAwait(false);
        return ok
            ? new ToolExecutionResult(Name, true, $"dmail_acked:{messageId}", Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>())
            : Fail($"message not found: {messageId}");
    }

    private static ToolExecutionResult Fail(string msg) =>
        new("dmail_ack", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
