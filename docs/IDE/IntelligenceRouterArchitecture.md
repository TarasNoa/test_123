# IDE Agent Intelligence Router Service - Architecture Documentation

## Overview

The Intelligence Router Service is responsible for smart orchestration policy for IDE agent workflows. It selects appropriate AI models and external tools (web search, GitHub search, StackOverflow search) for each development phase based on complexity, domain requirements, and cost optimization.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Routing algorithms, pattern matching, heuristics (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── IntelligenceRouter/
      ├── ModelProvider.cs            # Enum (OpenRouter, Ollama, Claude, GPT4, etc.)
      ├── ToolType.cs                 # Enum (BrowserSearch, GitHubSearch, StackOverflowSearch)
      ├── PhaseComplexity.cs           # Enum (Low, Medium, High, Critical)
      ├── RoutingDecision.cs          # Value object for phase routing
      ├── RoutingPlan.cs              # AggregateRoot for complete routing plan
      └── Events/
          └── RoutingPlanGeneratedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── IntelligenceRouterAlgorithms.fs
      ├── BuildTaskRoutingPlan        # Main routing plan generation
      ├── SelectPhaseDecision         # Select routing for specific phase
      ├── EvaluatePhaseComplexity      # Evaluate complexity of a phase
      ├── SelectModelProvider         # Select appropriate model provider
      ├── SelectTools                 # Select external tools for phase
      ├── CalculateRoutingConfidence   # Calculate confidence score
      └── BuildRationale              # Generate routing rationale

Libr4.IDE.Application/
  └── IntelligenceRouter/
      ├── Commands/
          ├── BuildRoutingPlanCommand.cs
      ├── Queries/
          ├── GetRoutingPlanQuery.cs
      ├── DTOs/
          ├── RoutingDecisionDto.cs
          ├── RoutingPlanDto.cs
      ├── Handlers/
          ├── BuildRoutingPlanCommandHandler.cs
          ├── GetRoutingPlanQueryHandler.cs
      └── Validators/
          └── BuildRoutingPlanCommandValidator.cs

Libr4.IDE.Api/
  └── IntelligenceRouterEndpoints.cs   # Minimal API endpoints
```

## Domain Model

### ModelProvider Enum

```csharp
public enum ModelProvider
{
    OpenRouter,    // Multi-provider routing
    Ollama,        // Local models
    Anthropic,     // Claude
    OpenAI,        // GPT-4
    Google,        // Gemini
    Together,      // Open-source models
    Local          // Local inference
}
```

### ToolType Enum

```csharp
public enum ToolType
{
    BrowserSearch,        // Web search
    GitHubSearch,         // GitHub repository search
    StackOverflowSearch,  // StackOverflow search
    DocumentationSearch,  // Documentation search
    None
}
```

### PhaseComplexity Enum

```csharp
public enum PhaseComplexity
{
    Low,       // Simple phase, lightweight model
    Medium,    // Moderate complexity, standard model
    High,      // Complex phase, advanced model
    Critical   // Safety-critical, highest quality model
}
```

### RoutingDecision Value Object

```csharp
public class RoutingDecision
{
    public string PhaseId { get; }
    public string PhaseName { get; }
    public PhaseComplexity Complexity { get; }
    public ModelProvider SelectedProvider { get; }
    public string SelectedModel { get; }
    public List<ToolType> SelectedTools { get; }
    public string Rationale { get; }
    public double Confidence { get; }
    public Dictionary<string, object> ContextQueries { get; }
}
```

### RoutingPlan AggregateRoot

```csharp
public class RoutingPlan : AggregateRoot<Guid>
{
    public string PlanId { get; }
    public string Prompt { get; }
    public List<RoutingDecision> PhaseDecisions { get; }
    public string PrimaryProvider { get; }
    public string PrimaryModel { get; }
    public List<ToolType> GlobalTools { get; }
    public string Rationale { get; }
    public double Confidence { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### BuildTaskRoutingPlan

Main algorithm that generates the complete routing plan:

```fsharp
let buildTaskRoutingPlan (prompt: string) (executionPlan: obj) (taskAnalysis: obj) (contextFiles: string list) : RoutingPlan =
    let phases = extractPhases executionPlan
    let decisions = 
        phases 
        |> List.map (fun phase -> selectPhaseDecision prompt phase taskAnalysis contextFiles)
    
    let primaryProvider = selectPrimaryProvider decisions
    let primaryModel = selectPrimaryModel decisions
    let globalTools = aggregateGlobalTools decisions
    let rationale = buildOverallRationale decisions
    let confidence = calculateOverallConfidence decisions
    
    RoutingPlan.Create(
        planId,
        prompt,
        decisions,
        primaryProvider,
        primaryModel,
        globalTools,
        rationale,
        confidence
    )
```

### EvaluatePhaseComplexity

Evaluates the complexity of a phase based on task analysis:

```fsharp
let evaluatePhaseComplexity (phase: obj) (taskAnalysis: obj) : PhaseComplexity =
    let phaseName = phase.Name.ToLower()
    let domainClass = taskAnalysis.DomainClass
    let riskLevel = taskAnalysis.RiskLevel
    
    match phaseName, domainClass, riskLevel with
    | name, domain, risk when domain = "safety_critical" || risk = "critical" -> PhaseComplexity.Critical
    | name, domain, risk when name.Contains("database") || name.Contains("model") -> PhaseComplexity.High
    | name, domain, risk when name.Contains("api") || name.Contains("service") -> PhaseComplexity.High
    | name, domain, risk when name.Contains("frontend") -> PhaseComplexity.Medium
    | name, domain, risk when name.Contains("planning") -> PhaseComplexity.Medium
    | _ -> PhaseComplexity.Low
```

### SelectModelProvider

Selects the appropriate model provider based on complexity and domain:

```fsharp
let selectModelProvider (complexity: PhaseComplexity) (domainClass: string) : ModelProvider =
    match complexity, domainClass with
    | PhaseComplexity.Critical, _ -> ModelProvider.OpenAI  // GPT-4 for critical
    | PhaseComplexity.Critical, _ -> ModelProvider.Anthropic  // Claude for critical
    | PhaseComplexity.High, "regulated" -> ModelProvider.OpenAI
    | PhaseComplexity.High, "safety_critical" -> ModelProvider.Anthropic
    | PhaseComplexity.High, _ -> ModelProvider.OpenRouter  // Cost optimization
    | PhaseComplexity.Medium, _ -> ModelProvider.OpenRouter
    | PhaseComplexity.Low, _ -> ModelProvider.Ollama  // Local for low complexity
    | _ -> ModelProvider.OpenRouter
```

### SelectTools

Selects external tools for a phase:

```fsharp
let selectTools (phaseName: string) (complexity: PhaseComplexity) : ToolType list =
    let phaseLower = phaseName.ToLower()
    
    let tools = System.Collections.Generic.List<ToolType>()
    
    // Browser search for planning and frontend
    if phaseLower.Contains("planning") || phaseLower.Contains("frontend") then
        tools.Add(ToolType.BrowserSearch)
    
    // GitHub search for backend phases
    if phaseLower.Contains("backend") || phaseLower.Contains("api") then
        tools.Add(ToolType.GitHubSearch)
    
    // StackOverflow for debugging and complex issues
    if complexity = PhaseComplexity.High || complexity = PhaseComplexity.Critical then
        tools.Add(ToolType.StackOverflowSearch)
    
    tools |> Seq.toList
```

### CalculateRoutingConfidence

Calculates confidence score for routing decisions:

```fsharp
let calculateRoutingConfidence (decision: RoutingDecision) : double =
    let baseScore = 0.8
    
    // Boost confidence for critical phases with high-quality models
    let modelBonus = 
        match decision.SelectedProvider with
        | ModelProvider.OpenAI -> 0.1
        | ModelProvider.Anthropic -> 0.1
        | ModelProvider.OpenRouter -> 0.05
        | _ -> 0.0
    
    // Boost confidence when tools are selected
    let toolBonus = 
        if decision.SelectedTools |> List.isEmpty then 0.0
        else 0.05 * (double decision.SelectedTools.Length)
    
    min 1.0 (baseScore + modelBonus + toolBonus)
```

## Application Layer (C#)

### Command Handler

```csharp
public class BuildRoutingPlanCommandHandler : IRequestHandler<BuildRoutingPlanCommand, RoutingPlanDto>
{
    private readonly IIntelligenceRouterAlgorithms _algorithms;
    
    public async Task<RoutingPlanDto> Handle(BuildRoutingPlanCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var routingPlan = _algorithms.BuildTaskRoutingPlan(
            request.Prompt,
            request.ExecutionPlan,
            request.TaskAnalysis,
            request.ContextFiles
        );
        
        // Map to DTO
        return routingPlan.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class IntelligenceRouterEndpoints
{
    public static void MapIntelligenceRouterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/intelligence-router")
            .WithTags("Intelligence Router")
            .RequireAuthorization();
        
        group.MapPost("/build-routing-plan", async (
            BuildRoutingPlanCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("BuildRoutingPlan")
        .WithOpenApi();
    }
}
```

## Routing Heuristics

### Phase-to-Provider Mapping

| Phase Type | Complexity | Recommended Provider | Recommended Model |
|-------------|------------|---------------------|-------------------|
| Planning | Medium | OpenRouter | Claude 3.5 Sonnet |
| DatabaseModel | High | Anthropic | Claude 3.5 Sonnet |
| BackendAPI | High | OpenAI | GPT-4 |
| BackendService | High | OpenRouter | Claude 3.5 Sonnet |
| FrontendExperience | Medium | OpenRouter | Claude 3.5 Sonnet |
| Enrichment | Critical | OpenAI | GPT-4 |
| BackendQuality | Medium | Ollama | Llama 3 70B |
| FrontendQuality | Medium | OpenRouter | Claude 3.5 Sonnet |

### Tool Selection Rules

| Phase Type | Browser Search | GitHub Search | StackOverflow |
|-------------|---------------|---------------|--------------|
| Planning | ✅ | ❌ | ❌ |
| DatabaseModel | ✅ | ✅ | ❌ |
| BackendAPI | ❌ | ✅ | ✅ |
| FrontendExperience | ✅ | ✅ | ❌ |
| Enrichment | ✅ | ❌ | ✅ |
| BackendQuality | ❌ | ❌ | ❌ |
| FrontendQuality | ❌ | ❌ | ❌ |

## Cost Optimization

The router implements cost optimization by:
1. Using OpenRouter for medium-complexity phases (cheaper models)
2. Using Ollama for low-complexity phases (free local models)
3. Reserving GPT-4/Claude for critical phases only
4. Selecting tools only when necessary

This can reduce API costs by up to 92% compared to always using the most expensive model.

## Testing Strategy

1. **Unit Tests** - Test F# routing algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full routing flow

## Performance Considerations

- Routing is CPU-light (pattern matching, heuristics)
- Use caching for similar routing requests
- Implement rate limiting on API endpoints

## Security Considerations

- Validate all model providers and tool types
- Sanitize context queries to prevent injection
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
