namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed record TriggerNormalizationResult(
    string Source,
    string AdapterName,
    string UserRequest,
    string? Actor,
    string? CorrelationId);

public interface ITriggerAdapter
{
    string Source { get; }
    bool CanHandle(string source);

    Task<TriggerNormalizationResult> NormalizeAsync(
        string source,
        string userRequest,
        string? actor,
        string? payloadJson,
        CancellationToken ct);
}

public interface ITriggerAdapterRouter
{
    Task<TriggerNormalizationResult> NormalizeAsync(
        string? source,
        string userRequest,
        string? actor,
        string? payloadJson,
        CancellationToken ct);
}
