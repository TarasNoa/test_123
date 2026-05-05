using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed class TriggerAdapterRouter : ITriggerAdapterRouter
{
    private readonly IReadOnlyList<ITriggerAdapter> _adapters;

    public TriggerAdapterRouter(IEnumerable<ITriggerAdapter> adapters)
    {
        _adapters = adapters.ToList();
    }

    public async Task<TriggerNormalizationResult> NormalizeAsync(
        string? source,
        string userRequest,
        string? actor,
        string? payloadJson,
        CancellationToken ct)
    {
        var src = string.IsNullOrWhiteSpace(source) ? "http" : source.Trim().ToLowerInvariant();
        var adapter = _adapters.FirstOrDefault(a => a.CanHandle(src))
                      ?? _adapters.First(a => a.CanHandle("http"));
        return await adapter.NormalizeAsync(src, userRequest, actor, payloadJson, ct);
    }
}

public sealed class HttpTriggerAdapter : ITriggerAdapter
{
    public string Source => "http";
    public bool CanHandle(string source) => string.Equals(source, "http", StringComparison.OrdinalIgnoreCase);

    public Task<TriggerNormalizationResult> NormalizeAsync(
        string source,
        string userRequest,
        string? actor,
        string? payloadJson,
        CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(new TriggerNormalizationResult(
            Source: "http",
            AdapterName: nameof(HttpTriggerAdapter),
            UserRequest: userRequest,
            Actor: actor,
            CorrelationId: TryExtractCorrelationId(payloadJson)));
    }

    private static string? TryExtractCorrelationId(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;
            if (doc.RootElement.TryGetProperty("correlationId", out var c) && c.ValueKind == JsonValueKind.String)
                return c.GetString();
        }
        catch
        {
            // Ignore malformed optional payload
        }
        return null;
    }
}

public sealed class SlackTriggerAdapter : ITriggerAdapter
{
    public string Source => "slack";
    public bool CanHandle(string source) => string.Equals(source, "slack", StringComparison.OrdinalIgnoreCase);

    public Task<TriggerNormalizationResult> NormalizeAsync(
        string source,
        string userRequest,
        string? actor,
        string? payloadJson,
        CancellationToken ct)
    {
        _ = payloadJson;
        _ = ct;
        return Task.FromResult(new TriggerNormalizationResult(
            Source: "slack",
            AdapterName: nameof(SlackTriggerAdapter),
            UserRequest: userRequest,
            Actor: actor,
            CorrelationId: null));
    }
}

public sealed class LinearTriggerAdapter : ITriggerAdapter
{
    public string Source => "linear";
    public bool CanHandle(string source) => string.Equals(source, "linear", StringComparison.OrdinalIgnoreCase);

    public Task<TriggerNormalizationResult> NormalizeAsync(
        string source,
        string userRequest,
        string? actor,
        string? payloadJson,
        CancellationToken ct)
    {
        _ = payloadJson;
        _ = ct;
        return Task.FromResult(new TriggerNormalizationResult(
            Source: "linear",
            AdapterName: nameof(LinearTriggerAdapter),
            UserRequest: userRequest,
            Actor: actor,
            CorrelationId: null));
    }
}

public sealed class GitHubTriggerAdapter : ITriggerAdapter
{
    public string Source => "github";
    public bool CanHandle(string source) => string.Equals(source, "github", StringComparison.OrdinalIgnoreCase);

    public Task<TriggerNormalizationResult> NormalizeAsync(
        string source,
        string userRequest,
        string? actor,
        string? payloadJson,
        CancellationToken ct)
    {
        _ = payloadJson;
        _ = ct;
        return Task.FromResult(new TriggerNormalizationResult(
            Source: "github",
            AdapterName: nameof(GitHubTriggerAdapter),
            UserRequest: userRequest,
            Actor: actor,
            CorrelationId: null));
    }
}
