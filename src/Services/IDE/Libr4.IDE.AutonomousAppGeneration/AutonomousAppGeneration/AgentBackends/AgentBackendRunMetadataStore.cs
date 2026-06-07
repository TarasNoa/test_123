using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;

public sealed class ExternalAgentBackendOptions
{
    public const string SectionName = "AutonomousAppGeneration:AgentBackends";

    public bool EnableNativeFallback { get; set; } = true;

    public int DefaultTimeoutSeconds { get; set; } = 900;

    public string CodexExecutable { get; set; } = "codex";

    public string OpenCodeExecutable { get; set; } = "opencode";

    public string NodeExecutable { get; set; } = "node";

    /// <summary>Path to cursor-sdk-agent.mjs (absolute or relative to repo root).</summary>
    public string CursorSdkRunnerScript { get; set; } = "scripts/cursor-sdk-agent.mjs";

    public string AcpExecutable { get; set; } = "acp-agent";

    /// <summary>Estimated cost recorded per external backend invocation when actual usage is unknown.</summary>
    public decimal ExternalBackendEstimatedCostUsd { get; set; } = 0.05m;

    public long ExternalBackendEstimatedTokens { get; set; } = 10_000;

    /// <summary>When non-empty, only listed backend slugs/kinds are allowed (e.g. libr4-native, codex-cli).</summary>
    public List<string> AllowedBackends { get; set; } = new();

    /// <summary>Run external CLI backends inside <see cref="Runtime.IIsolatedRuntime"/> (workspace bind-mount only).</summary>
    public bool IsolateExternalBackends { get; set; }

    /// <summary>Docker/WSL image for isolated external backend sessions.</summary>
    public string ExternalBackendRuntimeImage { get; set; } = "node:22-bookworm";
}

public sealed record AgentBackendRunMetadata(
    AgentBackendKind Backend,
    string BackendInstanceId,
    DateTime UpdatedAtUtc,
    AgentBackendKind? FallbackFrom = null,
    string? FallbackReason = null);

public static class AgentBackendRunMetadataStore
{
    public static async Task WriteAsync(
        string runsRoot,
        Guid runId,
        AgentBackendKind kind,
        string backendInstanceId,
        CancellationToken ct = default,
        AgentBackendKind? fallbackFrom = null,
        string? fallbackReason = null)
    {
        var dir = Path.Combine(Path.GetFullPath(runsRoot), runId.ToString("D"), "handoff");
        Directory.CreateDirectory(dir);
        var payload = new
        {
            backend = kind.ToString(),
            backendInstanceId,
            updatedAtUtc = DateTime.UtcNow,
            fallbackFrom = fallbackFrom?.ToString(),
            fallbackReason
        };
        await File.WriteAllTextAsync(
            Path.Combine(dir, "backend.json"),
            JsonSerializer.Serialize(payload),
            ct).ConfigureAwait(false);
    }

    public static AgentBackendRunMetadata? TryRead(string runsRoot, Guid runId)
    {
        var path = Path.Combine(Path.GetFullPath(runsRoot), runId.ToString("D"), "handoff", "backend.json");
        if (!File.Exists(path))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (!root.TryGetProperty("backend", out var backendEl))
                return null;

            if (!Enum.TryParse<AgentBackendKind>(backendEl.GetString(), true, out var kind))
                return null;

            var instanceId = root.TryGetProperty("backendInstanceId", out var instEl)
                ? instEl.GetString() ?? string.Empty
                : string.Empty;

            var updatedAt = root.TryGetProperty("updatedAtUtc", out var atEl)
                            && DateTime.TryParse(atEl.GetString(), out var parsed)
                ? parsed
                : DateTime.UtcNow;

            AgentBackendKind? fallbackFrom = null;
            if (root.TryGetProperty("fallbackFrom", out var fbEl)
                && Enum.TryParse<AgentBackendKind>(fbEl.GetString(), true, out var fbKind))
                fallbackFrom = fbKind;

            var fallbackReason = root.TryGetProperty("fallbackReason", out var reasonEl)
                ? reasonEl.GetString()
                : null;

            return new AgentBackendRunMetadata(kind, instanceId, updatedAt, fallbackFrom, fallbackReason);
        }
        catch
        {
            return null;
        }
    }

    public static AgentBackendKind? TryReadKind(string runsRoot, Guid runId) =>
        TryRead(runsRoot, runId)?.Backend;
}
