using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;

public interface IAgentBackendCoordinator
{
    Task<AgentSessionResult> RunSessionAsync(
        AgentBackendSpawnRequest request,
        CancellationToken ct = default);
}

public sealed class AgentBackendCoordinator : IAgentBackendCoordinator
{
    private readonly IAgentBackendRegistry _registry;
    private readonly AgentRuntimeOptions _runtimeOptions;
    private readonly ExternalAgentBackendOptions _options;
    private readonly IBudgetService? _budget;
    private readonly ILogger<AgentBackendCoordinator> _logger;

    public AgentBackendCoordinator(
        IAgentBackendRegistry registry,
        IOptions<AgentRuntimeOptions> runtimeOptions,
        IOptions<ExternalAgentBackendOptions> options,
        ILogger<AgentBackendCoordinator> logger,
        IBudgetService? budget = null)
    {
        _registry = registry;
        _runtimeOptions = runtimeOptions.Value;
        _options = options.Value;
        _budget = budget;
        _logger = logger;
    }

    public async Task<AgentSessionResult> RunSessionAsync(
        AgentBackendSpawnRequest request,
        CancellationToken ct = default)
    {
        EnsureBackendAllowed(request.Backend);

        try
        {
            var result = await RunInternalAsync(request, ct).ConfigureAwait(false);
            if (!result.Succeeded
                && _options.EnableNativeFallback
                && request.Backend.Kind != AgentBackendKind.Libr4Native)
            {
                _logger.LogWarning(
                    "External backend {Kind} failed for run {RunId}; falling back to Libr4Native",
                    request.Backend.Kind,
                    request.RunId);

                var fallback = request with { Backend = AgentBackendDescriptor.Native };
                var fallbackResult = await RunInternalAsync(
                    fallback,
                    ct,
                    fallbackFrom: request.Backend.Kind,
                    fallbackReason: result.Summary).ConfigureAwait(false);
                return fallbackResult;
            }

            return result;
        }
        catch (Exception ex) when (_options.EnableNativeFallback
                                  && request.Backend.Kind != AgentBackendKind.Libr4Native)
        {
            _logger.LogWarning(
                ex,
                "External backend {Kind} threw for run {RunId}; falling back to Libr4Native",
                request.Backend.Kind,
                request.RunId);

            var fallback = request with { Backend = AgentBackendDescriptor.Native };
            return await RunInternalAsync(
                fallback,
                ct,
                fallbackFrom: request.Backend.Kind,
                fallbackReason: ex.Message).ConfigureAwait(false);
        }
    }

    private void EnsureBackendAllowed(AgentBackendDescriptor backend)
    {
        if (_options.AllowedBackends.Count == 0)
            return;

        var slug = backend.Kind.ToString();
        var allowed = _options.AllowedBackends.Any(b =>
            string.Equals(b, slug, StringComparison.OrdinalIgnoreCase)
            || string.Equals(b, slug.Replace("Cli", "-cli", StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase));

        if (!allowed)
            throw new InvalidOperationException($"agent_backend_not_allowed:{slug}");
    }

    private async Task<AgentSessionResult> RunInternalAsync(
        AgentBackendSpawnRequest request,
        CancellationToken ct,
        AgentBackendKind? fallbackFrom = null,
        string? fallbackReason = null)
    {
        var backend = _registry.Resolve(request.Backend);
        var handle = await backend.SpawnAsync(request, ct).ConfigureAwait(false);

        await AgentBackendRunMetadataStore.WriteAsync(
            _runtimeOptions.RunsRoot,
            request.RunId,
            request.Backend.Kind,
            handle.BackendInstanceId,
            ct,
            fallbackFrom,
            fallbackReason).ConfigureAwait(false);

        var result = backend switch
        {
            Libr4NativeAgentBackend native =>
                await native.WaitForCompletionAsync(handle.BackendInstanceId, ct).ConfigureAwait(false),
            SubprocessCliAgentBackend cli =>
                await cli.WaitForCompletionAsync(handle.BackendInstanceId, ct).ConfigureAwait(false),
            _ => await WaitGenericAsync(backend, handle.BackendInstanceId, ct).ConfigureAwait(false)
        };

        await RecordBackendCostAsync(request, result, ct).ConfigureAwait(false);
        return result;
    }

    private async Task RecordBackendCostAsync(
        AgentBackendSpawnRequest request,
        AgentSessionResult result,
        CancellationToken ct)
    {
        if (_budget is null)
            return;

        var backendKind = request.Backend.Kind.ToString();
        var stage = $"backend:{backendKind.ToLowerInvariant()}";

        if (request.Backend.Kind == AgentBackendKind.Libr4Native)
        {
            var usage = _budget.GetUsage(request.RunId);
            var assignedTokens = _budget.GetBackendUsage(request.RunId).Values.Sum(b => b.TokensUsed);
            var assignedCost = _budget.GetBackendUsage(request.RunId).Values.Sum(b => b.CostUsdUsed);
            var deltaTokens = Math.Max(0, usage.TokensUsed - assignedTokens);
            var deltaCost = Math.Max(0m, usage.CostUsdUsed - assignedCost);
            _budget.AttributeUsageToBackend(request.RunId, backendKind, deltaTokens, deltaCost);
            return;
        }

        var tokens = _options.ExternalBackendEstimatedTokens;
        var cost = _options.ExternalBackendEstimatedCostUsd;
        if (!result.Succeeded)
            cost *= 0.5m;

        await _budget.TryConsumeAsync(
            request.RunId,
            stage,
            tokens,
            cost,
            backendKind,
            ct).ConfigureAwait(false);
    }

    private static async Task<AgentSessionResult> WaitGenericAsync(
        IAgentBackend backend,
        string instanceId,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var status = await backend.GetStatusAsync(instanceId, ct).ConfigureAwait(false);
            if (status.Status is AgentBackendRunStatus.Completed)
            {
                return new AgentSessionResult(
                    true,
                    status.Stage ?? "completed",
                    Array.Empty<Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile>(),
                    status.StepNumber ?? 0,
                    Array.Empty<string>());
            }

            if (status.Status is AgentBackendRunStatus.Failed or AgentBackendRunStatus.Cancelled)
            {
                return new AgentSessionResult(
                    false,
                    status.Error ?? status.Status.ToString(),
                    Array.Empty<Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile>(),
                    status.StepNumber ?? 0,
                    Array.Empty<string>());
            }

            await Task.Delay(250, ct).ConfigureAwait(false);
        }

        throw new OperationCanceledException(ct);
    }
}
