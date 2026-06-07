using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;

public static class AgentBackendEventMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ToNdjsonLine(AgentBackendEvent evt)
    {
        var type = evt.Kind switch
        {
            AgentBackendEventKind.Message => "message",
            AgentBackendEventKind.ToolUse => "tool_use",
            AgentBackendEventKind.Status => "status",
            AgentBackendEventKind.Cost => "cost",
            AgentBackendEventKind.Error => "error",
            _ => "status"
        };

        var payload = new
        {
            type,
            runId = evt.RunId,
            backendInstanceId = evt.BackendInstanceId,
            timestampUtc = evt.TimestampUtc,
            payload = JsonSerializer.Deserialize<JsonElement>(evt.PayloadJson)
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static AgentBackendEvent CreateStatusEvent(
        Guid runId,
        string backendInstanceId,
        string stage,
        int? stepNumber = null) =>
        new(
            AgentBackendEventKind.Status,
            runId,
            backendInstanceId,
            DateTime.UtcNow,
            JsonSerializer.Serialize(new { stage, stepNumber }, JsonOptions));

    public static AgentBackendEvent CreateMessageEvent(
        Guid runId,
        string backendInstanceId,
        string role,
        string content) =>
        new(
            AgentBackendEventKind.Message,
            runId,
            backendInstanceId,
            DateTime.UtcNow,
            JsonSerializer.Serialize(new { role, content }, JsonOptions));
}
