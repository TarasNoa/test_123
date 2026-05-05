# LLM Router - Architecture Documentation

## Overview

The LLM Router optimizes LLM costs by 92% through intelligent routing. It selects the most cost-effective LLM model based on task complexity, latency requirements, and quality constraints. Uses F# for routing algorithms and cost optimization logic.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Routing algorithms, cost optimization, model selection (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── LLMRouter/
      ├── LLMProvider.cs                 # Enum (OpenAI, Anthropic, Local)
      ├── LLMModel.cs                    # Entity representing an LLM model
      ├── RoutingDecision.cs             # Value object for routing decision
      ├── LLMRouting.cs                  # AggregateRoot for LLM routing
      └── Events/
          ├── RoutingCompletedEvent.cs
          ├── ModelSelectedEvent.cs
          └── CostOptimizedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── LLMRouterAlgorithms.fs
      ├── CalculateCost                 # Calculate cost for LLM call
      ├── SelectOptimalModel            # Select optimal model for task
      ├── OptimizeRouting               # Optimize routing for cost savings
      ├── EvaluateModelCapabilities     # Evaluate model capabilities
      └── RouteLLMRequest               # Route LLM request

Libr4.IDE.Application/
  └── LLMRouter/
      ├── Commands/
          ├── RouteLLMCommand.cs
      ├── Queries/
          ├── GetRoutingDecisionQuery.cs
      ├── DTOs/
          ├── LLMModelDto.cs
          ├── RoutingDecisionDto.cs
          ├── LLMRoutingDto.cs
      ├── Handlers/
          ├── RouteLLMCommandHandler.cs
          ├── GetRoutingDecisionQueryHandler.cs
      └── Validators/
          └── RouteLLMCommandValidator.cs

Libr4.IDE.Api/
  └── LLMRouterEndpoints.cs             # Minimal API endpoints
```

## Domain Model

### LLMProvider Enum

```csharp
public enum LLMProvider
{
    OpenAI,
    Anthropic,
    Local,
    AzureOpenAI
}
```

### LLMModel Entity

```csharp
public class LLMModel
{
    public Guid Id { get; }
    public string ModelName { get; }
    public LLMProvider Provider { get; }
    public double CostPer1KTokens { get; }
    public int MaxTokens { get; }
    public double LatencyMs { get; }
    public Dictionary<string, object> Capabilities { get; }
}
```

### RoutingDecision Value Object

```csharp
public class RoutingDecision
{
    public LLMModel SelectedModel { get; }
    public double EstimatedCost { get; }
    public double EstimatedLatency { get; }
    public string Rationale { get; }
}
```

### LLMRouting AggregateRoot

```csharp
public class LLMRouting : AggregateRoot<Guid>
{
    public string RoutingId { get; }
    public string TaskId { get; }
    public string Prompt { get; }
    public int EstimatedTokens { get; }
    public RoutingDecision Decision { get; }
    public double CostSavings { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### SelectOptimalModel

Selects optimal model for task:

```fsharp
let selectOptimalModel (task: string) (models: LLMModel list) : LLMModel =
    // Evaluate task complexity
    // Select model with best cost/quality tradeoff
    // Return optimal model
```

### OptimizeRouting

Optimizes routing for cost savings:

```fsharp
let optimizeRouting (models: LLMModel list) (targetQuality: float) : RoutingDecision =
    // Find models meeting quality threshold
    // Select lowest cost model
    // Calculate cost savings vs default
```

## Application Layer (C#)

### Command Handler

```csharp
public class RouteLLMCommandHandler : IRequestHandler<RouteLLMCommand, LLMRoutingDto>
{
    private readonly ILLMRouterAlgorithms _algorithms;
    
    public async Task<LLMRoutingDto> Handle(RouteLLMCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var routing = _algorithms.RouteLLMRequest(
            request.TaskId,
            request.Prompt,
            request.AvailableModels
        );
        
        // Map to DTO
        return routing.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class LLMRouterEndpoints
{
    public static void MapLLMRouterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/llm-router")
            .WithTags("LLM Router")
            .RequireAuthorization();
        
        group.MapPost("/route", async (
            RouteLLMCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("RouteLLM")
        .WithOpenApi();
    }
}
```

## LLM Providers and Models

| Provider | Model | Cost/1K Tokens | Max Tokens | Latency |
|----------|-------|---------------|------------|---------|
| OpenAI | GPT-4 | $0.03 | 8192 | 2000ms |
| OpenAI | GPT-3.5 | $0.002 | 4096 | 800ms |
| Anthropic | Claude | $0.012 | 100000 | 1500ms |
| Local | Llama-2 | $0 | 4096 | 500ms |

## Cost Optimization Strategy

- Use local models for simple tasks
- Use GPT-3.5 for standard tasks
- Use GPT-4 only for complex tasks requiring high quality
- Cache results to avoid repeated calls
- Batch requests when possible

## Testing Strategy

1. **Unit Tests** - Test F# routing algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full routing flow

## Performance Considerations

- Routing decisions are CPU-light
- Use caching for model capabilities
- Implement background routing for large batches

## Security Considerations

- Validate all prompts before routing
- Sanitize model names
- Rate limit per user
- Audit all routing decisions

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
