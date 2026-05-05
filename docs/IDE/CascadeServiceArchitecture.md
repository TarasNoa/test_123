# IDE Agent Cascade Service - Architecture Documentation

## Overview

The Cascade Service performs an orchestrator pass after task decomposition. It generates an orchestration plan JSON by prompting an LLM with task details and phase information, optionally enriched with web search prefetch results. This ensures the AI has a coherent understanding of the entire multi-phase workflow before executing individual phases.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Cascade planning algorithms, orchestration logic (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── Cascade/
      ├── OrchestratorPhase.cs         # Value object representing a phase in orchestrator output
      ├── OrchestratorPlan.cs          # AggregateRoot for the complete orchestrator plan
      ├── PrefetchContext.cs           # Value object for web prefetch context
      └── Events/
          └── CascadePlanGeneratedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── CascadeAlgorithms.fs
      ├── RunCascadePlanning          # Main cascade planning algorithm
      ├── BuildOrchestratorPrompt     # Build prompt for orchestrator LLM
      ├── FormatPhaseForPrompt        # Format phase information for prompt
      ├── EnrichWithWebPrefetch       # Enrich prompt with web search results
      ├── ParseOrchestratorOutput     # Parse LLM output into structured plan
      └── ValidateOrchestratorPlan    # Validate the generated plan

Libr4.IDE.Application/
  └── Cascade/
      ├── Commands/
          ├── RunCascadePlanningCommand.cs
      ├── Queries/
          ├── GetCascadePlanQuery.cs
      ├── DTOs/
          ├── OrchestratorPhaseDto.cs
          ├── OrchestratorPlanDto.cs
      ├── Handlers/
          ├── RunCascadePlanningCommandHandler.cs
          ├── GetCascadePlanQueryHandler.cs
      └── Validators/
          └── RunCascadePlanningCommandValidator.cs

Libr4.IDE.Api/
  └── CascadeEndpoints.cs             # Minimal API endpoints
```

## Domain Model

### OrchestratorPhase Value Object

```csharp
public class OrchestratorPhase
{
    public string PhaseId { get; }
    public string PhaseName { get; }
    public string Description { get; }
    public List<string> Dependencies { get; }
    public Dictionary<string, object> PhaseSpecificInstructions { get; }
    public string ExpectedOutput { get; }
}
```

### PrefetchContext Value Object

```csharp
public class PrefetchContext
{
    public bool PrefetchEnabled { get; }
    public List<WebSearchResult> WebSearchResults { get; }
    public Dictionary<string, string> DocumentationReferences { get; }
    public DateTime PrefetchedAt { get; }
}
```

### OrchestratorPlan AggregateRoot

```csharp
public class OrchestratorPlan : AggregateRoot<Guid>
{
    public string PlanId { get; }
    public string OriginalPrompt { get; }
    public TaskAnalysis TaskAnalysis { get; }
    public List<OrchestratorPhase> Phases { get; }
    public PrefetchContext PrefetchContext { get; }
    public string OrchestratorJson { get; }
    public string Rationale { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### RunCascadePlanning

Main algorithm that orchestrates the cascade planning:

```fsharp
let runCascadePlanning (prompt: string) (executionPlan: obj) (taskAnalysis: obj) (prefetchWeb: bool) : OrchestratorPlan =
    // Step 1: Format phases for prompt
    let formattedPhases = formatPhasesForPrompt executionPlan
    
    // Step 2: Optionally prefetch web context
    let prefetchContext = 
        if prefetchWeb then
            enrichWithWebPrefetch prompt formattedPhases
        else
            PrefetchContext.Empty
    
    // Step 3: Build orchestrator prompt
    let orchestratorPrompt = buildOrchestratorPrompt prompt formattedPhases taskAnalysis prefetchContext
    
    // Step 4: Call LLM for orchestrator output
    let orchestratorOutput = callLLM orchestratorPrompt
    
    // Step 5: Parse and validate output
    let orchestratorPlan = parseOrchestratorOutput orchestratorOutput
    let validatedPlan = validateOrchestratorPlan orchestratorPlan
    
    validatedPlan
```

### BuildOrchestratorPrompt

Builds the prompt for the orchestrator LLM:

```fsharp
let buildOrchestratorPrompt (prompt: string) (phases: string list) (taskAnalysis: obj) (prefetchContext: PrefetchContext) : string =
    let domainContext = 
        match taskAnalysis.DomainClass with
        | "regulated" -> "This is a regulated domain (banking/fintech). Ensure compliance, audit trails, and fail-safe patterns."
        | "safety_critical" -> "This is a safety-critical domain. Ensure explicit failure modes, approvals, and audit logging."
        | _ -> "This is a production SaaS application. Ensure reliability, maintainability, and clarity."
    
    let webContext = 
        if prefetchContext.PrefetchEnabled then
            sprintf "\n\nWeb Prefetch Context:\n%s" (formatWebContext prefetchContext.WebSearchResults)
        else
            ""
    
    sprintf """You are an orchestrator for a multi-phase AI development workflow. Your job is to coordinate the execution phases and ensure coherence across phases.

Original Request: %s

Domain Context: %s%s

Phases to execute:
%s

Output a JSON orchestrator plan with the following structure:
{
  "phases": [
    {
      "phase_id": "planning",
      "phase_name": "Planning",
      "description": "Brief description",
      "dependencies": [],
      "phase_specific_instructions": {},
      "expected_output": "What this phase should produce"
    }
  ],
  "rationale": "Overall rationale for the orchestration"
}

Reply with JSON only, no markdown fences."""
        prompt domainContext webContext (phases |> String.concat "\n")
```

### FormatPhaseForPrompt

Formats phase information for the orchestrator prompt:

```fsharp
let formatPhaseForPrompt (phase: obj) : string =
    sprintf "%s: %s (complexity: %s)" 
        phase.PhaseId 
        phase.Name 
        phase.Complexity
```

### EnrichWithWebPrefetch

Enriches the prompt with web search results:

```fsharp
let enrichWithWebPrefetch (prompt: string) (phases: string list) : PrefetchContext =
    let searchQuery = sprintf "%s %s" prompt (phases |> String.concat " ")
    
    // Call web search service
    let searchResults = webSearchService.Search searchQuery
    
    PrefetchContext.Create(
        true,
        searchResults,
        extractDocumentationReferences searchResults,
        DateTime.UtcNow
    )
```

### ParseOrchestratorOutput

Parses the LLM output into a structured plan:

```fsharp
let parseOrchestratorOutput (output: string) : OrchestratorPlan =
    try
        let json = JsonDocument.Parse output
        let phases = parsePhases json.RootElement.GetProperty "phases"
        let rationale = json.RootElement.GetProperty "rationale".GetString()
        
        OrchestratorPlan.Create(phases, rationale)
    with ex ->
        // Fallback: create a default plan
        createDefaultOrchestratorPlan()
```

## Application Layer (C#)

### Command Handler

```csharp
public class RunCascadePlanningCommandHandler : IRequestHandler<RunCascadePlanningCommand, OrchestratorPlanDto>
{
    private readonly ICascadeAlgorithms _algorithms;
    
    public async Task<OrchestratorPlanDto> Handle(RunCascadePlanningCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var orchestratorPlan = _algorithms.RunCascadePlanning(
            request.Prompt,
            request.ExecutionPlan,
            request.TaskAnalysis,
            request.PrefetchWeb
        );
        
        // Map to DTO
        return orchestratorPlan.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class CascadeEndpoints
{
    public static void MapCascadeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/cascade")
            .WithTags("Cascade")
            .RequireAuthorization();
        
        group.MapPost("/run-cascade-planning", async (
            RunCascadePlanningCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("RunCascadePlanning")
        .WithOpenApi();
    }
}
```

## Cascade Planning Flow

```
User Request
    ↓
Task Decomposition
    ↓
Cascade Planning
    ├─→ Format Phases for Prompt
    ├─→ [Optional] Web Prefetch
    ├─→ Build Orchestrator Prompt
    ├─→ Call LLM
    ├─→ Parse Output
    └─→ Validate Plan
    ↓
Orchestrator Plan (JSON)
    ↓
Phase Execution
```

## Web Prefetch Integration

The cascade service can optionally prefetch web context to enrich the orchestrator's understanding:

1. **Search Query Generation**: Combines original prompt with phase keywords
2. **Web Search**: Calls web search service (Tavily, Brave, etc.)
3. **Result Filtering**: Filters relevant documentation and examples
4. **Context Injection**: Injects filtered results into orchestrator prompt

This ensures the orchestrator has up-to-date information about:
- Best practices for the technology stack
- Recent documentation updates
- Common patterns and anti-patterns
- Security considerations

## Testing Strategy

1. **Unit Tests** - Test F# cascade algorithms with mock LLM responses
2. **Integration Tests** - Test C# Application layer with real LLM (or mock)
3. **E2E Tests** - Test full cascade flow from prompt to plan

## Performance Considerations

- Cascade planning involves LLM calls (can be slow)
- Use caching for similar cascade requests
- Implement streaming response for long-running cascades
- Background processing for large tasks

## Security Considerations

- Validate all LLM outputs before parsing
- Sanitize web search results before injection
- Rate limit per user
- Audit all cascade planning requests

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
