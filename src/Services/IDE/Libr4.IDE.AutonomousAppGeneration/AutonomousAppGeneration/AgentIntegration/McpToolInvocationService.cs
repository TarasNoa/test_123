using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class McpToolInvocationService : IMcpToolInvocationService
{
    private enum QuotaMode
    {
        PerRun,
        Standalone,
    }

    private readonly IMcpToolRegistry _registry;
    private readonly IMcpExecutionPolicy _policy;
    private readonly IMcpSessionRouter _router;
    private readonly IOptions<McpExecutionOptions> _mcpOptions;
    private readonly IMcpServerPreflight _preflight;
    private readonly ILogger<McpToolInvocationService> _logger;
    private readonly ConcurrentDictionary<Guid, RunMcpCounters> _counters = new();
    private int _standaloneInFlight;

    public McpToolInvocationService(
        IMcpToolRegistry registry,
        IMcpExecutionPolicy policy,
        IMcpSessionRouter router,
        IOptions<McpExecutionOptions> mcpOptions,
        IMcpServerPreflight preflight,
        ILogger<McpToolInvocationService> logger)
    {
        _registry = registry;
        _policy = policy;
        _router = router;
        _mcpOptions = mcpOptions;
        _preflight = preflight;
        _logger = logger;
    }

    public Task<McpInvocationOutcome> InvokeAsync(
        AppGenerationOrchestrator orchestrator,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken ct) =>
        InvokeCoreAsync(
            orchestrator,
            orchestrator.UserRequest,
            toolName,
            arguments,
            QuotaMode.PerRun,
            orchestrator.Id,
            ct);

    public Task<McpInvocationOutcome> InvokeStandaloneAsync(
        string? userRequestContext,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken ct) =>
        InvokeCoreAsync(
            orchestrator: null,
            userRequestContext,
            toolName,
            arguments,
            QuotaMode.Standalone,
            runId: null,
            ct);

    private async Task<McpInvocationOutcome> InvokeCoreAsync(
        AppGenerationOrchestrator? orchestrator,
        string? userRequestForRouter,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        QuotaMode quotaMode,
        Guid? runId,
        CancellationToken ct)
    {
        var opt = _mcpOptions.Value;
        var started = DateTime.UtcNow;
        var routerCtx = userRequestForRouter ?? string.Empty;
        var metaOutcome = await TryHandleMetaToolAsync(
            orchestrator,
            userRequestForRouter,
            toolName,
            arguments,
            quotaMode,
            runId,
            started,
            ct).ConfigureAwait(false);
        if (metaOutcome is not null)
            return metaOutcome;

        var meta = _registry.FindTool(toolName);
        if (meta is null)
        {
            if (orchestrator is not null)
            {
                Record(orchestrator, toolName, "unknown", McpExecutionLaneKind.Internal, McpToolRiskLevel.Low,
                    HashArgs(arguments), started, 0, "registry_miss", "Tool not registered");
            }

            return new McpInvocationOutcome(false, "registry_miss", "Tool not registered", null);
        }

        var argsJson = JsonSerializer.Serialize(arguments);
        var lane = _router.ResolveLane(meta, routerCtx);
        if (IsLaneKillSwitchActive(lane, opt))
        {
            if (orchestrator is not null)
            {
                Record(orchestrator, meta.ToolName, meta.ServerProfileKey, lane, meta.Risk,
                    HashArgs(arguments), started, 0, "lane_kill_switch", LaneKey(lane));
            }

            return new McpInvocationOutcome(false, "lane_kill_switch",
                $"{LaneKey(lane)} lane disabled by configuration", null);
        }

        // Preflight check for MCP server availability
        if (!string.IsNullOrEmpty(meta.ServerProfileKey))
        {
            var preflightResult = _preflight.CheckServerAvailability(meta.ServerProfileKey);
            if (!preflightResult.IsAvailable)
            {
                if (orchestrator is not null)
                {
                    Record(orchestrator, meta.ToolName, meta.ServerProfileKey, lane, meta.Risk,
                        HashArgs(arguments), started, 0, preflightResult.BlockerCode ?? "mcp_server_unavailable",
                        preflightResult.DiagnosticMessage ?? "MCP server unavailable");
                }

                return new McpInvocationOutcome(false, preflightResult.BlockerCode ?? "mcp_server_unavailable",
                    preflightResult.DiagnosticMessage ?? "MCP server unavailable", null);
            }
        }

        var policy = _policy.Evaluate(meta, argsJson);
        if (!policy.Allowed)
        {
            if (orchestrator is not null)
            {
                Record(orchestrator, meta.ToolName, meta.ServerProfileKey, lane, meta.Risk,
                    HashArgs(arguments), started, 0, "policy_denied", policy.Detail);
            }

            return new McpInvocationOutcome(false, "policy_denied", policy.Detail, null);
        }

        var standaloneLease = 0;
        if (quotaMode == QuotaMode.Standalone && opt.MaxConcurrentStandaloneInvocations > 0)
        {
            var v = Interlocked.Increment(ref _standaloneInFlight);
            if (v > opt.MaxConcurrentStandaloneInvocations)
            {
                Interlocked.Decrement(ref _standaloneInFlight);
                return new McpInvocationOutcome(false, "standalone_concurrency",
                    "Too many concurrent standalone MCP invocations", null);
            }

            standaloneLease = 1;
        }

        RunMcpCounters? counters = null;
        if (quotaMode == QuotaMode.PerRun && runId is Guid rid)
            counters = _counters.GetOrAdd(rid, _ => new RunMcpCounters());

        try
        {
            if (counters is not null)
            {
                if (counters.TotalCalls + 1 > opt.MaxToolCallsPerRun)
                {
                    if (orchestrator is not null)
                    {
                        Record(orchestrator, meta.ToolName, meta.ServerProfileKey, lane, meta.Risk,
                            HashArgs(arguments), started, 0, "quota_exceeded", "MaxToolCallsPerRun");
                    }

                    return new McpInvocationOutcome(false, "quota_exceeded", "Run tool-call budget exhausted", null);
                }

                var laneKey = LaneKey(lane);
                counters.LaneCalls.TryGetValue(laneKey, out var lc);
                var laneLimit = opt.LaneMaxCalls.TryGetValue(laneKey, out var maxLane) ? maxLane : int.MaxValue;
                if (lc + 1 > laneLimit)
                {
                    if (orchestrator is not null)
                    {
                        Record(orchestrator, meta.ToolName, meta.ServerProfileKey, lane, meta.Risk,
                            HashArgs(arguments), started, 0, "lane_quota_exceeded", laneKey);
                    }

                    return new McpInvocationOutcome(false, "lane_quota_exceeded", $"Lane {laneKey} quota exceeded", null);
                }

                counters.TotalCalls++;
                counters.LaneCalls[laneKey] = lc + 1;
            }

            if (!opt.EnableStdioTransport)
            {
                if (orchestrator is not null)
                {
                    Record(orchestrator, meta.ToolName, meta.ServerProfileKey, lane, meta.Risk,
                        HashArgs(arguments), started, 0, "transport_disabled", null);
                }

                return new McpInvocationOutcome(true, "transport_disabled",
                    orchestrator is null
                        ? "MCP stdio transport disabled (no audit row for standalone calls)"
                        : "MCP stdio transport disabled; audit row recorded on the run", null);
            }

            if (!opt.ServerProfiles.TryGetValue(meta.ServerProfileKey, out var profile))
            {
                if (orchestrator is not null)
                {
                    Record(orchestrator, meta.ToolName, meta.ServerProfileKey, lane, meta.Risk,
                        HashArgs(arguments), started, 0, "profile_missing", meta.ServerProfileKey);
                }

                return new McpInvocationOutcome(false, "profile_missing",
                    $"No ServerProfiles entry for '{meta.ServerProfileKey}'", null);
            }

            if (!string.IsNullOrWhiteSpace(profile.WorkingDirectory) &&
                !Directory.Exists(profile.WorkingDirectory))
            {
                if (orchestrator is not null)
                {
                    Record(orchestrator, meta.ToolName, meta.ServerProfileKey, lane, meta.Risk,
                        HashArgs(arguments), started, 0, "working_directory_missing",
                        $"Working directory '{profile.WorkingDirectory}' does not exist");
                }

                return new McpInvocationOutcome(false, "working_directory_missing",
                    $"Working directory '{profile.WorkingDirectory}' does not exist", null);
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var timeout = TimeSpan.FromMilliseconds(Math.Clamp(opt.DefaultTimeoutMs, 1_000, 600_000));
                var result = await McpStdioJsonRpc.CallToolAsync(profile, toolName, arguments, timeout, ct)
                    .ConfigureAwait(false);
                sw.Stop();
                var summary = McpResultNormalizer.Normalize(result);
                if (orchestrator is not null)
                {
                    Record(orchestrator, meta.ToolName, meta.ServerProfileKey, lane, meta.Risk,
                        HashArgs(arguments), started, sw.ElapsedMilliseconds, "succeeded", null);
                }

                _logger.LogInformation(
                    "MCP tool {Tool} on server {Server} succeeded in {Ms}ms",
                    toolName, meta.ServerProfileKey, sw.ElapsedMilliseconds);
                return new McpInvocationOutcome(true, "succeeded", null, summary);
            }
            catch (Exception ex)
            {
                sw.Stop();
                var msg = ex.Message.Length > 512 ? ex.Message[..512] : ex.Message;

                if (orchestrator is not null)
                {
                    Record(orchestrator, meta.ToolName, meta.ServerProfileKey, lane, meta.Risk,
                        HashArgs(arguments), started, sw.ElapsedMilliseconds, "transport_error", msg);
                }

                _logger.LogWarning(ex, "MCP tool {Tool} failed", toolName);
                return new McpInvocationOutcome(false, "transport_error", msg, null);
            }
        }
        finally
        {
            if (standaloneLease == 1)
                Interlocked.Decrement(ref _standaloneInFlight);
        }
    }

    private static string LaneKey(McpExecutionLaneKind lane) => lane switch
    {
        McpExecutionLaneKind.Browser => "browser",
        McpExecutionLaneKind.N8n => "n8n",
        McpExecutionLaneKind.Workflow => "workflow",
        _ => "internal",
    };

    private static bool IsLaneKillSwitchActive(McpExecutionLaneKind lane, McpExecutionOptions opt) =>
        lane switch
        {
            McpExecutionLaneKind.Browser => opt.KillSwitchBrowserLane,
            McpExecutionLaneKind.N8n => opt.KillSwitchN8nLane,
            McpExecutionLaneKind.Workflow => opt.KillSwitchWorkflowLane,
            _ => false,
        };

    private void Record(
        AppGenerationOrchestrator orchestrator,
        string toolName,
        string serverName,
        McpExecutionLaneKind lane,
        McpToolRiskLevel risk,
        string argsHash,
        DateTime startedAtUtc,
        long durationMs,
        string outcome,
        string? detail)
    {
        orchestrator.RecordMcpExecution(new McpExecutionAuditEntry(
            toolName,
            serverName,
            lane,
            risk,
            argsHash,
            startedAtUtc,
            durationMs,
            outcome,
            detail));
    }

    private static string HashArgs(IReadOnlyDictionary<string, object?> args)
    {
        var normalized = args
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        var json = JsonSerializer.Serialize(normalized);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task<McpInvocationOutcome?> TryHandleMetaToolAsync(
        AppGenerationOrchestrator? orchestrator,
        string? userRequestForRouter,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        QuotaMode quotaMode,
        Guid? runId,
        DateTime started,
        CancellationToken ct)
    {
        if (!toolName.StartsWith("mcp.", StringComparison.OrdinalIgnoreCase))
            return null;

        switch (toolName.ToLowerInvariant())
        {
            case "mcp.list":
            {
                var laneFilter = GetStringArgument(arguments, "lane");
                var scopeFilter = GetStringArgument(arguments, "scope");
                var tools = _registry.ListTools()
                    .Where(t =>
                        (string.IsNullOrWhiteSpace(laneFilter) || t.Lane.ToString().Equals(laneFilter, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrWhiteSpace(scopeFilter) || t.Scopes.Any(s => s.Equals(scopeFilter, StringComparison.OrdinalIgnoreCase))))
                    .Select(t => new
                    {
                        tool = t.ToolName,
                        server = t.ServerProfileKey,
                        lane = t.Lane.ToString(),
                        risk = t.Risk.ToString(),
                        description = t.Description,
                        scopes = t.Scopes
                    })
                    .ToList();
                var summary = JsonSerializer.Serialize(tools);
                RecordMetaTool(orchestrator, toolName, started, "succeeded", null);
                return new McpInvocationOutcome(true, "succeeded", null, summary);
            }
            case "mcp.search":
            {
                var query = (GetStringArgument(arguments, "query") ?? string.Empty).Trim();
                if (query.Length == 0)
                {
                    RecordMetaTool(orchestrator, toolName, started, "bad_request", "query is required");
                    return new McpInvocationOutcome(false, "bad_request", "query is required", null);
                }

                var q = query.ToLowerInvariant();
                var tools = _registry.ListTools()
                    .Where(t =>
                        t.ToolName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        t.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        t.ServerProfileKey.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        t.Scopes.Any(s => s.Contains(query, StringComparison.OrdinalIgnoreCase)))
                    .Select(t => new
                    {
                        tool = t.ToolName,
                        server = t.ServerProfileKey,
                        lane = t.Lane.ToString(),
                        risk = t.Risk.ToString(),
                        description = t.Description,
                        rank = ComputeSearchRank(t, q)
                    })
                    .OrderByDescending(x => x.rank)
                    .ThenBy(x => x.tool, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var summary = JsonSerializer.Serialize(tools);
                RecordMetaTool(orchestrator, toolName, started, "succeeded", null);
                return new McpInvocationOutcome(true, "succeeded", null, summary);
            }
            case "mcp.describe":
            {
                var requested = GetStringArgument(arguments, "tool") ?? GetStringArgument(arguments, "toolName");
                if (string.IsNullOrWhiteSpace(requested))
                {
                    RecordMetaTool(orchestrator, toolName, started, "bad_request", "tool is required");
                    return new McpInvocationOutcome(false, "bad_request", "tool is required", null);
                }

                var meta = _registry.FindTool(requested);
                if (meta is null)
                {
                    RecordMetaTool(orchestrator, toolName, started, "registry_miss", $"Tool not found: {requested}");
                    return new McpInvocationOutcome(false, "registry_miss", $"Tool not found: {requested}", null);
                }

                var detail = JsonSerializer.Serialize(new
                {
                    tool = meta.ToolName,
                    server = meta.ServerProfileKey,
                    lane = meta.Lane.ToString(),
                    risk = meta.Risk.ToString(),
                    description = meta.Description,
                    scopes = meta.Scopes
                });
                RecordMetaTool(orchestrator, toolName, started, "succeeded", null);
                return new McpInvocationOutcome(true, "succeeded", null, detail);
            }
            case "mcp.call":
            {
                var targetTool = GetStringArgument(arguments, "tool") ?? GetStringArgument(arguments, "toolName");
                if (string.IsNullOrWhiteSpace(targetTool))
                {
                    RecordMetaTool(orchestrator, toolName, started, "bad_request", "tool is required");
                    return new McpInvocationOutcome(false, "bad_request", "tool is required", null);
                }

                if (targetTool.StartsWith("mcp.", StringComparison.OrdinalIgnoreCase))
                {
                    RecordMetaTool(orchestrator, toolName, started, "bad_request", "nested mcp.* calls are not allowed");
                    return new McpInvocationOutcome(false, "bad_request", "nested mcp.* calls are not allowed", null);
                }

                var innerArgs = ExtractNestedArguments(arguments);
                var proxied = await InvokeCoreAsync(
                    orchestrator,
                    userRequestForRouter,
                    targetTool,
                    innerArgs,
                    quotaMode,
                    runId,
                    ct).ConfigureAwait(false);
                RecordMetaTool(orchestrator, toolName, started, proxied.OutcomeCode, proxied.Detail);
                return proxied with
                {
                    OutcomeCode = $"proxied:{proxied.OutcomeCode}"
                };
            }
            default:
                RecordMetaTool(orchestrator, toolName, started, "registry_miss", "Unknown MCP meta-tool");
                return new McpInvocationOutcome(false, "registry_miss", "Unknown MCP meta-tool", null);
        }
    }

    private static int ComputeSearchRank(McpToolMetadata meta, string queryLower)
    {
        var score = 0;
        if (meta.ToolName.Equals(queryLower, StringComparison.OrdinalIgnoreCase))
            score += 100;
        if (meta.ToolName.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
            score += 50;
        if (meta.Description.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
            score += 20;
        if (meta.Scopes.Any(s => s.Contains(queryLower, StringComparison.OrdinalIgnoreCase)))
            score += 10;
        return score;
    }

    private static IReadOnlyDictionary<string, object?> ExtractNestedArguments(IReadOnlyDictionary<string, object?> args)
    {
        if (!args.TryGetValue("arguments", out var raw) || raw is null)
            return new Dictionary<string, object?>();

        if (raw is IReadOnlyDictionary<string, object?> ro)
            return ro;
        if (raw is Dictionary<string, object?> rw)
            return rw;
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Object)
        {
            var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in je.EnumerateObject())
                map[prop.Name] = JsonElementToObject(prop.Value);
            return map;
        }

        return new Dictionary<string, object?>();
    }

    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var i) => i,
            JsonValueKind.Number when element.TryGetDouble(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    private static string? GetStringArgument(IReadOnlyDictionary<string, object?> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var raw) || raw is null)
            return null;
        if (raw is string s)
            return s;
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.String)
            return je.GetString();
        return raw.ToString();
    }

    private static void RecordMetaTool(
        AppGenerationOrchestrator? orchestrator,
        string toolName,
        DateTime started,
        string outcome,
        string? detail)
    {
        if (orchestrator is null)
            return;

        orchestrator.RecordMcpExecution(new McpExecutionAuditEntry(
            ToolName: toolName,
            ServerName: "mcp-meta",
            Lane: McpExecutionLaneKind.Internal,
            RiskLevel: McpToolRiskLevel.Low,
            ArgumentsSha256: "meta",
            StartedAtUtc: started,
            DurationMs: 0,
            Outcome: outcome,
            Detail: detail));
    }

    private sealed class RunMcpCounters
    {
        public int TotalCalls;
        public readonly Dictionary<string, int> LaneCalls = new(StringComparer.OrdinalIgnoreCase);
    }
}
