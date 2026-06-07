using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;

public sealed class McpRunHostManager : IMcpRunHostManager, IDisposable
{
    private sealed class SessionEntry
    {
        public required McpHostTransportKind Transport { get; init; }
        public McpStdioSession? Stdio { get; init; }
        public McpSseTransport? Sse { get; init; }
        public DateTime StartedAtUtc { get; init; }
        public DateTime LastUsedAtUtc { get; set; }
        public int CallCount { get; set; }
    }

    private readonly IOptions<McpHostOptions> _hostOptions;
    private readonly IOptions<McpExecutionOptions> _mcpOptions;
    private readonly IMcpExternalServerDiscovery _discovery;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<McpRunHostManager> _logger;
    private readonly ConcurrentDictionary<(Guid RunId, string ProfileKey), SessionEntry> _sessions = new();
    private readonly SemaphoreSlim _standaloneGate = new(1, 1);
    private McpStdioSession? _standaloneStdio;
    private string? _standaloneProfileKey;

    public McpRunHostManager(
        IOptions<McpHostOptions> hostOptions,
        IOptions<McpExecutionOptions> mcpOptions,
        IMcpExternalServerDiscovery discovery,
        IHttpClientFactory httpClientFactory,
        ILogger<McpRunHostManager> logger)
    {
        _hostOptions = hostOptions;
        _mcpOptions = mcpOptions;
        _discovery = discovery;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public bool IsUnifiedHostEnabled => _hostOptions.Value.EnableUnifiedHost;

    public async Task<JsonElement> CallToolAsync(
        Guid? runId,
        string profileKey,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var effectiveRunId = runId ?? Guid.Empty;
        var entry = await GetOrCreateSessionAsync(effectiveRunId, profileKey, ct).ConfigureAwait(false);
        entry.LastUsedAtUtc = DateTime.UtcNow;
        entry.CallCount++;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        if (entry.Transport == McpHostTransportKind.Sse && entry.Sse is not null)
            return await entry.Sse.CallToolAsync(toolName, arguments, timeout, cts.Token).ConfigureAwait(false);

        if (entry.Stdio is not null)
            return await entry.Stdio.CallToolAsync(toolName, arguments, cts.Token).ConfigureAwait(false);

        throw new InvalidOperationException($"MCP host session missing transport for profile '{profileKey}'");
    }

    public void ReleaseRun(Guid runId)
    {
        var keys = _sessions.Keys.Where(k => k.RunId == runId).ToList();
        foreach (var key in keys)
        {
            if (_sessions.TryRemove(key, out var entry))
                _ = DisposeEntryAsync(entry);
        }

        _logger.LogInformation("Released MCP host sessions for run {RunId} ({Count} profiles)", runId, keys.Count);
    }

    public IReadOnlyList<McpRunHostSessionInfo> ListActiveSessions() =>
        _sessions.Select(kv => new McpRunHostSessionInfo(
            kv.Key.RunId,
            kv.Key.ProfileKey,
            kv.Value.Transport,
            kv.Value.StartedAtUtc,
            kv.Value.LastUsedAtUtc,
            kv.Value.CallCount)).ToList();

    public IReadOnlyList<McpServerDiscoveryResult> DiscoverServers() =>
        _discovery.DiscoverAsync().GetAwaiter().GetResult();

    public void Dispose()
    {
        foreach (var entry in _sessions.Values)
            _ = DisposeEntryAsync(entry);
        _sessions.Clear();
        if (_standaloneStdio is not null)
            _ = _standaloneStdio.DisposeAsync();
    }

    internal async Task EvictIdleSessionsAsync(CancellationToken ct)
    {
        var idle = TimeSpan.FromMinutes(Math.Max(1, _hostOptions.Value.RunSessionIdleTimeoutMinutes));
        var cutoff = DateTime.UtcNow - idle;
        foreach (var kv in _sessions.ToArray())
        {
            if (kv.Value.LastUsedAtUtc >= cutoff)
                continue;
            if (_sessions.TryRemove(kv.Key, out var entry))
            {
                await DisposeEntryAsync(entry).ConfigureAwait(false);
                _logger.LogDebug(
                    "Evicted idle MCP session run={RunId} profile={Profile}",
                    kv.Key.RunId,
                    kv.Key.ProfileKey);
            }
        }
    }

    private async Task<SessionEntry> GetOrCreateSessionAsync(Guid runId, string profileKey, CancellationToken ct)
    {
        if (runId != Guid.Empty
            && _sessions.TryGetValue((runId, profileKey), out var existing))
            return existing;

        if (runId == Guid.Empty)
            return await GetStandaloneSessionAsync(profileKey, ct).ConfigureAwait(false);

        var created = await CreateSessionAsync(profileKey, ct).ConfigureAwait(false);
        return _sessions.GetOrAdd((runId, profileKey), created);
    }

    private async Task<SessionEntry> GetStandaloneSessionAsync(string profileKey, CancellationToken ct)
    {
        await _standaloneGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_standaloneStdio is not null
                && string.Equals(_standaloneProfileKey, profileKey, StringComparison.OrdinalIgnoreCase))
            {
                return new SessionEntry
                {
                    Transport = McpHostTransportKind.Stdio,
                    Stdio = _standaloneStdio,
                    StartedAtUtc = DateTime.UtcNow,
                    LastUsedAtUtc = DateTime.UtcNow,
                };
            }

            if (_standaloneStdio is not null)
            {
                await _standaloneStdio.DisposeAsync().ConfigureAwait(false);
                _standaloneStdio = null;
            }

            var entry = await CreateSessionAsync(profileKey, ct).ConfigureAwait(false);
            if (entry.Stdio is not null)
            {
                _standaloneStdio = entry.Stdio;
                _standaloneProfileKey = profileKey;
            }

            return entry;
        }
        finally
        {
            _standaloneGate.Release();
        }
    }

    private async Task<SessionEntry> CreateSessionAsync(string profileKey, CancellationToken ct)
    {
        var host = _hostOptions.Value;
        if (host.EnableSseTransport && host.SseServers.TryGetValue(profileKey, out var sseProfile))
        {
            var client = _httpClientFactory.CreateClient($"McpSse:{profileKey}");
            return new SessionEntry
            {
                Transport = McpHostTransportKind.Sse,
                Sse = new McpSseTransport(client, sseProfile),
                StartedAtUtc = DateTime.UtcNow,
                LastUsedAtUtc = DateTime.UtcNow,
            };
        }

        if (!host.EnableStdioTransport)
            throw new InvalidOperationException("MCP stdio transport disabled in McpHostOptions");

        if (!_mcpOptions.Value.ServerProfiles.TryGetValue(profileKey, out var launch))
            throw new InvalidOperationException($"No MCP server profile '{profileKey}'");

        var stdio = await McpStdioSession.StartAsync(launch, ct).ConfigureAwait(false);
        return new SessionEntry
        {
            Transport = McpHostTransportKind.Stdio,
            Stdio = stdio,
            StartedAtUtc = DateTime.UtcNow,
            LastUsedAtUtc = DateTime.UtcNow,
        };
    }

    private static async Task DisposeEntryAsync(SessionEntry entry)
    {
        if (entry.Stdio is not null)
            await entry.Stdio.DisposeAsync().ConfigureAwait(false);
    }
}

public sealed class McpRunHostJanitor : BackgroundService
{
    private readonly McpRunHostManager _manager;
    private readonly ILogger<McpRunHostJanitor> _logger;

    public McpRunHostJanitor(McpRunHostManager manager, ILogger<McpRunHostJanitor> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _manager.EvictIdleSessionsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "MCP run host janitor iteration failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
        }
    }
}
