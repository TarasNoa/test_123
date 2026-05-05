# IDE Task Decomposition Service - Architecture Documentation

## Overview

The Task Decomposition Service is the core component of the IDE Agent system. It breaks down complex user requests into executable phases, enabling safe, structured AI operations.

## Architecture

### Technology Stack

- **C#** - Domain models, API, EF Core, validation (Infrastructure)
- **F#** - Task decomposition algorithms, pattern matching, functional logic (Algorithms)
- **PostgreSQL** - Persistence (via EF Core)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── TaskDecomposition/
      ├── OperationType.cs           # Enum (FeatureAddition, BugFix, Refactoring, etc.)
      ├── ComplexityLevel.cs          # Enum (Low, Medium, High, Extreme)
      ├── TaskAnalysis.cs             # Domain entity
      ├── ExecutionPhase.cs           # Domain entity
      ├── ExecutionPlan.cs            # AggregateRoot
      └── Events/
          ├── TaskDecomposedEvent.cs
          └── PhaseGeneratedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── TaskDecompositionAlgorithms.fs
      ├── DecomposeTask               # Main decomposition algorithm
      ├── AnalyzeTask                # Task analysis (operation type, complexity)
      ├── GeneratePhases              # Phase generation
      ├── OptimizePhases             # Phase optimization
      ├── AssessRisk                 # Risk assessment
      └── InferDomainClass           # Domain classification (regulated, safety-critical)

Libr4.IDE.Application/
  └── TaskDecomposition/
      ├── Commands/
          ├── DecomposeTaskCommand.cs
      ├── Queries/
          ├── GetExecutionPlanQuery.cs
      ├── DTOs/
          ├── TaskAnalysisDto.cs
          ├── ExecutionPhaseDto.cs
          ├── ExecutionPlanDto.cs
      ├── Handlers/
          ├── DecomposeTaskCommandHandler.cs
          ├── GetExecutionPlanQueryHandler.cs
      └── Validators/
          └── DecomposeTaskCommandValidator.cs

Libr4.IDE.Api/
  └── TaskDecompositionEndpoints.cs   # Minimal API endpoints
```

## Domain Model

### OperationType Enum

```csharp
public enum OperationType
{
    FeatureAddition,    // Add new functionality
    BugFix,            // Fix bugs
    Refactoring,       // Restructure code
    Optimization,      // Improve performance
    Testing,           // Add tests
    Configuration,     // Configure settings
    Documentation      // Add documentation
}
```

### ComplexityLevel Enum

```csharp
public enum ComplexityLevel
{
    Low,      // Single file, simple changes
    Medium,   // Multiple files, moderate complexity
    High,     // Complex multi-file changes, high risk
    Extreme   // Major restructuring, very high risk
}
```

### TaskAnalysis Entity

```csharp
public class TaskAnalysis
{
    public Guid Id { get; private set; }
    public OperationType OperationType { get; private set; }
    public ComplexityLevel Complexity { get; private set; }
    public List<string> KeyConcepts { get; private set; }
    public List<string> AffectedComponents { get; private set; }
    public int EstimatedFiles { get; private set; }
    public string RiskLevel { get; private set; }
    public bool TestingRequired { get; private set; }
    public string DomainClass { get; private set; }  // standard, regulated, safety_critical
    public bool FullAppBuildRequest { get; private set; }
    public bool RichImplementationRequired { get; private set; }
}
```

### ExecutionPhase Entity

```csharp
public class ExecutionPhase
{
    public Guid Id { get; private set; }
    public string PhaseId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public List<string> Files { get; private set; }
    public int EstimatedComplexity { get; private set; }
    public List<string> Dependencies { get; private set; }
    public List<ValidationStep> ValidationSteps { get; private set; }
    public string RollbackStrategy { get; private set; }
}
```

### ExecutionPlan AggregateRoot

```csharp
public class ExecutionPlan : AggregateRoot<Guid>
{
    public string PlanId { get; private set; }
    public TaskAnalysis TaskAnalysis { get; private set; }
    public List<ExecutionPhase> Phases { get; private set; }
    public int TotalEstimatedComplexity { get; private set; }
    public string RiskAssessment { get; private set; }
    public string RollbackStrategy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int EstimatedDurationMinutes { get; private set; }
}
```

## F# Algorithms

### DecomposeTask

Main algorithm that orchestrates the decomposition process:

```fsharp
let decomposeTask (prompt: string) (workspaceId: string) (contextFiles: string list) : ExecutionPlan =
    // Step 1: Analyze the task
    let taskAnalysis = analyzeTask prompt
    
    // Step 2: Gather context and constraints
    let context = gatherContext prompt workspaceId contextFiles
    
    // Step 3: Generate execution phases
    let phases = generatePhases taskAnalysis context
    
    // Step 4: Validate and optimize plan
    let optimizedPhases = optimizePhases phases
    
    // Step 5: Calculate overall metrics
    let totalComplexity = optimizedPhases |> List.sumBy (fun p -> p.EstimatedComplexity)
    let riskAssessment = assessRisk taskAnalysis optimizedPhases
    
    // Build execution plan
    ExecutionPlan.Create(taskAnalysis, optimizedPhases, totalComplexity, riskAssessment)
```

### AnalyzeTask

Uses pattern matching to determine operation type and complexity:

```fsharp
let analyzeTask (prompt: string) : TaskAnalysis =
    let operationType = 
        match prompt.ToLower() with
        | s when s.Contains("add") || s.Contains("implement") || s.Contains("create") -> FeatureAddition
        | s when s.Contains("fix") || s.Contains("resolve") || s.Contains("patch") -> BugFix
        | s when s.Contains("refactor") || s.Contains("restructure") -> Refactoring
        | s when s.Contains("optimize") || s.Contains("improve") -> Optimization
        | _ -> FeatureAddition
    
    let complexity = 
        match prompt.Split(' ').Length, contextFiles.Length with
        | (words, files) when words < 10 && files < 3 -> Low
        | (words, files) when words < 20 && files < 10 -> Medium
        | (words, files) when words < 40 && files < 20 -> High
        | _ -> Extreme
    
    let domainClass = inferDomainClass prompt
    
    TaskAnalysis.Create(operationType, complexity, domainClass)
```

### InferDomainClass

Classifies the task domain:

```fsharp
let inferDomainClass (prompt: string) : string =
    let lowerPrompt = prompt.ToLower()
    
    let safetyCriticalKeywords = [
        "spacecraft"; "satellite"; "telemetry"; "mission control"
        "спутник"; "телеметр"; "аномал"
    ]
    
    let regulatedKeywords = [
        "bank"; "banking"; "payment"; "wallet"; "fintech"; "kyc"; "fraud"
        "банк"; "банкинг"; "платеж"; "финтех"
    ]
    
    if safetyCriticalKeywords |> List.exists (fun kw -> lowerPrompt.Contains(kw)) then
        "safety_critical"
    elif regulatedKeywords |> List.exists (fun kw -> lowerPrompt.Contains(kw)) then
        "regulated"
    else
        "standard"
```

## Application Layer (C#)

### Command Handler

```csharp
public class DecomposeTaskCommandHandler : IRequestHandler<DecomposeTaskCommand, ExecutionPlanDto>
{
    private readonly ITaskDecompositionAlgorithms _algorithms;
    private readonly ITaskDecompositionRepository _repository;
    
    public async Task<ExecutionPlanDto> Handle(DecomposeTaskCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var executionPlan = _algorithms.DecomposeTask(
            request.Prompt,
            request.WorkspaceId,
            request.ContextFiles
        );
        
        // Persist to database
        await _repository.AddAsync(executionPlan, ct);
        
        // Publish domain event
        executionPlan.AddDomainEvent(new TaskDecomposedEvent(executionPlan.Id));
        
        // Map to DTO
        return executionPlan.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class TaskDecompositionEndpoints
{
    public static void MapTaskDecompositionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/task-decomposition")
            .WithTags("Task Decomposition")
            .RequireAuthorization();
        
        group.MapPost("/decompose", async (
            DecomposeTaskCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("DecomposeTask")
        .WithOpenApi();
        
        group.MapGet("/plans/{planId}", async (
            Guid planId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetExecutionPlanQuery(planId);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetExecutionPlan")
        .WithOpenApi();
    }
}
```

## Domain Events

### TaskDecomposedEvent

```csharp
public class TaskDecomposedEvent : IDomainEvent
{
    public Guid ExecutionPlanId { get; }
    public DateTime OccurredOn { get; }
    
    public TaskDecomposedEvent(Guid executionPlanId)
    {
        ExecutionPlanId = executionPlanId;
        OccurredOn = DateTime.UtcNow;
    }
}
```

## Validation

### FluentValidation

```csharp
public class DecomposeTaskCommandValidator : AbstractValidator<DecomposeTaskCommand>
{
    public DecomposeTaskCommandValidator()
    {
        RuleFor(x => x.Prompt)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(10000);
        
        RuleFor(x => x.WorkspaceId)
            .NotEmpty();
        
        RuleFor(x => x.ContextFiles)
            .Must(x => x == null || x.Count <= 100)
            .WithMessage("Cannot process more than 100 context files");
    }
}
```

## Testing Strategy

1. **Unit Tests** - Test F# algorithms in isolation
2. **Integration Tests** - Test C# Application layer with in-memory database
3. **E2E Tests** - Test full flow from API to database

## Performance Considerations

- Task decomposition is CPU-intensive (LLM calls)
- Use caching for similar tasks
- Implement rate limiting on API endpoints
- Background processing for large tasks

## Security Considerations

- Validate all user input
- Sanitize file paths
- Rate limit per user
- Audit all decompositions

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
