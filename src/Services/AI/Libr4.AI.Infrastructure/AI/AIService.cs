using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Libr4.AI.Infrastructure.Hooks;
using Libr4.AI.Infrastructure.LLM;
using Libr4.AI.Domain.Memory.Enhanced.FSharp;

namespace Libr4.AI.Infrastructure.AI;

/// <summary>
/// P1-5 (audit roadmap): AIService now wraps every provider call with a per-provider
/// circuit breaker (<see cref="LlmCircuitBreaker"/>). An open circuit throws
/// <see cref="LlmCircuitOpenException"/> so callers can apply fallback logic.
/// Enhanced with hooks, session tracking, memory, and smart routing.
/// </summary>
public class AIService : IAIService
{
    private readonly AIProviderFactory _providerFactory;
    private readonly IConfiguration _configuration;
    private readonly LlmCircuitBreaker _circuitBreaker;
    private readonly ILogger<AIService> _logger;
    private readonly HookManager _hookManager;
    private readonly LLMRouter _router;

    private string? _currentSessionId;
    private string? _currentUserId;

    public AIService(
        AIProviderFactory providerFactory,
        IConfiguration configuration,
        LlmCircuitBreaker circuitBreaker,
        ILogger<AIService> logger,
        HookManager hookManager,
        LLMRouter router)
    {
        _providerFactory = providerFactory;
        _configuration = configuration;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
        _hookManager = hookManager;
        _router = router;
    }

    public void SetSessionContext(string sessionId, string? userId = null)
    {
        _currentSessionId = sessionId;
        _currentUserId = userId;
    }

    public async Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null)
    {
        // Execute pre-tool hooks
        var hookContext = new HookContext
        {
            SessionId = _currentSessionId ?? "default",
            ToolName = "GenerateCompletion",
            Parameters = new Dictionary<string, object> { ["prompt"] = prompt, ["model"] = model ?? "default" },
            UserId = _currentUserId
        };

        await _hookManager.ExecuteHooksAsync(HookType.PreToolUse, hookContext);

        // Smart routing if model not specified
        if (string.IsNullOrEmpty(model))
        {
            var decision = _router.Route(
                task: "completion",
                context: prompt,
                requiredFeatures: new List<string> { "coding", "reasoning" },
                maxCost: 0.01);
            model = decision.ModelId;
            _logger.LogInformation("Routed to model {ModelId} with confidence {Confidence}", model, decision.Confidence);
        }

        var result = await ExecuteWithCircuitBreakerAsync(
            () =>
            {
                var provider = _providerFactory.GetProvider();
                return provider.GenerateCompletionAsync(prompt, systemPrompt, model);
            });

        hookContext.Result = result;
        await _hookManager.ExecuteHooksAsync(HookType.PostToolUse, hookContext);

        return result;
    }

    public async Task<string> GenerateEmbeddingAsync(string text, string? model = null)
    {
        var hookContext = new HookContext
        {
            SessionId = _currentSessionId ?? "default",
            ToolName = "GenerateEmbedding",
            Parameters = new Dictionary<string, object> { ["text"] = text, ["model"] = model ?? "default" },
            UserId = _currentUserId
        };

        await _hookManager.ExecuteHooksAsync(HookType.PreToolUse, hookContext);

        var result = await ExecuteWithCircuitBreakerAsync(
            () =>
            {
                var provider = _providerFactory.GetProvider();
                return provider.GenerateEmbeddingAsync(text, model);
            });

        hookContext.Result = result;
        await _hookManager.ExecuteHooksAsync(HookType.PostToolUse, hookContext);

        return result;
    }

    public async Task<string> AnalyzeTextAsync(string text, string analysisType, string? model = null)
    {
        var hookContext = new HookContext
        {
            SessionId = _currentSessionId ?? "default",
            ToolName = "AnalyzeText",
            Parameters = new Dictionary<string, object> { ["text"] = text, ["type"] = analysisType, ["model"] = model ?? "default" },
            UserId = _currentUserId
        };

        await _hookManager.ExecuteHooksAsync(HookType.PreToolUse, hookContext);

        var result = await ExecuteWithCircuitBreakerAsync(
            () =>
            {
                var provider = _providerFactory.GetProvider();
                return provider.AnalyzeTextAsync(text, analysisType, model);
            });

        hookContext.Result = result;
        await _hookManager.ExecuteHooksAsync(HookType.PostToolUse, hookContext);

        return result;
    }

    public async Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null)
    {
        // Generate session ID if not set
        if (string.IsNullOrEmpty(_currentSessionId))
        {
            _currentSessionId = Guid.NewGuid().ToString();
        }

        var hookContext = new HookContext
        {
            SessionId = _currentSessionId ?? "default",
            ToolName = "Chat",
            Parameters = new Dictionary<string, object> { ["message"] = message, ["model"] = model ?? "default" },
            UserId = _currentUserId
        };

        await _hookManager.ExecuteHooksAsync(HookType.PreToolUse, hookContext);

        // Smart routing for chat
        if (string.IsNullOrEmpty(model))
        {
            var decision = _router.Route(
                task: "chat",
                context: message,
                requiredFeatures: new List<string> { "reasoning", "tools" },
                maxCost: 0.01);
            model = decision.ModelId;
            _logger.LogInformation("Routed to model {ModelId} for chat", model);
        }

        var result = await ExecuteWithCircuitBreakerAsync(
            () =>
            {
                var provider = _providerFactory.GetProvider();
                return provider.ChatAsync(message, systemPrompt, model);
            });

        hookContext.Result = result;
        await _hookManager.ExecuteHooksAsync(HookType.PostToolUse, hookContext);

        return result;
    }

    // ── Circuit breaker wrapper ───────────────────────────────────────────────

    private async Task<string> ExecuteWithCircuitBreakerAsync(Func<Task<string>> operation)
    {
        var providerId = _configuration["AI:DefaultProvider"] ?? "default";

        if (_circuitBreaker.IsOpen(providerId))
        {
            _logger.LogWarning(
                "[CircuitBreaker] Circuit is OPEN for provider {ProviderId}. Rejecting request.",
                providerId);
            throw new LlmCircuitOpenException(providerId);
        }

        try
        {
            var result = await operation();
            _circuitBreaker.OnSuccess(providerId);
            return result;
        }
        catch (LlmCircuitOpenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _circuitBreaker.OnFailure(providerId);
            _logger.LogWarning(ex,
                "[CircuitBreaker] Provider {ProviderId} call failed. Failure recorded.",
                providerId);
            throw;
        }
    }
}

/// <summary>Thrown when a call is rejected because the provider circuit is open.</summary>
public sealed class LlmCircuitOpenException(string providerId)
    : Exception($"LLM circuit breaker is open for provider '{providerId}'. Requests are being rejected until the circuit recovers.")
{
    public string ProviderId { get; } = providerId;
}
