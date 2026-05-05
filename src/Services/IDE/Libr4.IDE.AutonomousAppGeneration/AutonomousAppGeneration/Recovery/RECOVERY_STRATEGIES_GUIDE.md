# Recovery Strategies Guide

## Overview

The recovery cascade provides a self-healing mechanism for LLM pipeline errors. Strategies are attempted in order until one succeeds or all are exhausted.

## Adding a New Recovery Strategy

### Step 1: Implement IRecoveryStrategy

Create a new class that implements the `IRecoveryStrategy` interface:

```csharp
public class MyCustomRecoveryStrategy : IRecoveryStrategy
{
    public bool CanRecover(Exception exception, RecoveryContext context)
    {
        // Determine if this strategy can handle the given exception
        // Return true if applicable, false otherwise
        return exception is MySpecificException;
    }

    public async Task<RecoveryResult> RecoverAsync(RecoveryContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Implement your recovery logic here
            // Modify context as needed
            
            return new RecoveryResult
            {
                Success = true,
                StrategyUsed = "MyCustomRecovery",
                Reason = "Successfully recovered",
                ContextAfterRecovery = context,
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            return new RecoveryResult
            {
                Success = false,
                StrategyUsed = "MyCustomRecovery",
                Reason = $"Failed: {ex.Message}",
                ContextAfterRecovery = context,
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    public string GetStrategyName()
    {
        return "MyCustomRecovery";
    }
}
```

### Step 2: Register the Strategy

Add your strategy to the `RecoveryCascadeService` constructor:

```csharp
_strategies = new List<IRecoveryStrategy>
{
    new ContextMicroCompressionRecoveryStrategy(),
    new ContextCollapseRecoveryStrategy(),
    new TokenEscalationRecoveryStrategy(),
    new FallbackModelRecoveryStrategy(_options.FallbackModel),
    new MyCustomRecoveryStrategy() // Add your strategy here
};
```

### Step 3: Configure Strategy Order

Strategies are attempted in the order they are registered. Consider the following when ordering:

1. **Fast strategies first** - Strategies that are quick to execute should come first
2. **Low impact first** - Strategies that minimally modify context should be tried before aggressive ones
3. **Specific before general** - Handle specific error types before falling back to general strategies

### Step 4: Add Tests

Create integration tests for your strategy in `RecoveryCascadeServiceTests.cs`:

```csharp
[Fact]
public async Task MyCustomRecoveryStrategy_ShouldHandleSpecificError()
{
    var strategy = new MyCustomRecoveryStrategy();
    var context = new RecoveryContext
    {
        CurrentPrompt = "Some prompt",
        MessageHistory = new List<string> { "Message 1" }
    };

    var exception = new MySpecificException("Error occurred");
    var result = await strategy.RecoverAsync(context);

    result.Success.Should().BeTrue();
    result.StrategyUsed.Should().Be("MyCustomRecovery");
}
```

## Existing Strategies

### ContextMicroCompressionRecoveryStrategy
- **Purpose**: Removes least significant messages from context
- **When to use**: Token limit errors
- **Impact**: Moderate - removes 20-30% of messages

### ContextCollapseRecoveryStrategy
- **Purpose**: Collapses groups of messages into summaries
- **When to use**: Large context with many messages
- **Impact**: High - reduces message count significantly

### TokenEscalationRecoveryStrategy
- **Purpose**: Adds continuation hint to prompt
- **When to use**: Output truncation errors
- **Impact**: Low - only adds hint text

### FallbackModelRecoveryStrategy
- **Purpose**: Switches to backup model
- **When to use**: Provider errors, timeouts
- **Impact**: High - changes the entire LLM model

## Circuit Breaker

The circuit breaker pattern prevents repeatedly trying strategies that consistently fail:

- Strategies are skipped after `CircuitBreakerThreshold` consecutive failures
- Failure count is reset on successful recovery
- Use `ResetCircuitBreakers()` to manually reset (useful for testing)

## Caching

Recovery results are cached based on:
- Exception type
- Current token count
- Message history count
- Recovery attempt number

This prevents redundant recovery attempts for identical error contexts.

Use `ClearCache()` to manually clear the cache when context changes significantly.

## Best Practices

1. **Keep strategies idempotent** - Multiple applications should produce the same result
2. **Log recovery actions** - Include detailed logging for debugging
3. **Handle cancellation** - Respect the CancellationToken parameter
4. **Measure performance** - Track duration to ensure strategies don't add excessive latency
5. **Test edge cases** - Test with empty contexts, single messages, maximum limits
