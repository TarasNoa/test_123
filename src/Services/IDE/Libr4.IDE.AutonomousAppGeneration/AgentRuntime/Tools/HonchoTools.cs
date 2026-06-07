using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Honcho;
using Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class HonchoProfileTool : IAgentTool
{
    private readonly IHonchoMemoryService _honcho;

    public HonchoProfileTool(IHonchoMemoryService honcho) => _honcho = honcho;

    public string Name => "honcho_profile";
    public string Description =>
        "Read project persona for current user. Input: { \"project_key\"?, \"user_id\"? }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var userId = ReadString(input, "user_id")
                     ?? context.Session.TenantUserId
                     ?? "anonymous";
        var projectKey = ReadString(input, "project_key") ?? ResolveProjectKey(context);

        var persona = await _honcho.GetPersonaAsync(userId, projectKey, ct).ConfigureAwait(false);
        if (persona is null)
            return Ok("(no persona yet)");

        return Ok(persona.ToPlanningSection(4000));
    }

    private static string ResolveProjectKey(ToolContext context) =>
        WorkspaceTrustHasher.Compute(context.Workspace.HostPath, context.Session.TenantUserId, context.Session.SessionId);

    private static string? ReadString(JsonElement input, string name) =>
        input.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private ToolExecutionResult Ok(string output) =>
        new(Name, true, output, Array.Empty<GeneratedFile>());

    private ToolExecutionResult Fail(string reason) =>
        new(Name, false, reason, Array.Empty<GeneratedFile>());
}

public sealed class HonchoReasoningTool : IAgentTool
{
    private readonly IHonchoMemoryService _honcho;

    public HonchoReasoningTool(IHonchoMemoryService honcho) => _honcho = honcho;

    public string Name => "honcho_reasoning";
    public string Description =>
        "Ask Honcho dialectic about the user. Input: { \"query\", \"project_key\"?, \"user_id\"?, \"session_id\"? }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var query = ReadString(input, "query");
        if (string.IsNullOrWhiteSpace(query))
            return Fail("query is required");

        var userId = ReadString(input, "user_id")
                     ?? context.Session.TenantUserId
                     ?? "anonymous";
        var projectKey = ReadString(input, "project_key") ?? ResolveProjectKey(context);
        var sessionId = ReadString(input, "session_id") ?? context.Session.SessionId;

        var result = await _honcho.ReasonAsync(userId, projectKey, sessionId, query!, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(result.Content))
            return Ok("(no honcho answer)");

        return Ok(result.Content);
    }

    private static string ResolveProjectKey(ToolContext context) =>
        WorkspaceTrustHasher.Compute(context.Workspace.HostPath, context.Session.TenantUserId, context.Session.SessionId);

    private static string? ReadString(JsonElement input, string name) =>
        input.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private ToolExecutionResult Ok(string output) =>
        new(Name, true, output, Array.Empty<GeneratedFile>());

    private ToolExecutionResult Fail(string reason) =>
        new(Name, false, reason, Array.Empty<GeneratedFile>());
}
