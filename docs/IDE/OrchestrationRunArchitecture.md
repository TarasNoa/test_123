# IDE Agent Orchestration Run Service - Architecture Documentation

## Overview

The Orchestration Run Service manages the lifecycle of agent orchestration runs, including skill selection and workflow transitions. It defines default skills (plan_request, multi_file_edit, validation_loop, qa_automation) and handles workflow hook milestones for tracking execution progress.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Orchestration algorithms, skill selection, workflow transitions (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── OrchestrationRun/
      ├── SkillType.cs                  # Enum (PlanRequest, MultiFileEdit, ValidationLoop, etc.)
      ├── Skill.cs                     # Value object representing a skill
      ├── WorkflowTransition.cs         # Value object for workflow transitions
      ├── OrchestrationRun.cs          # AggregateRoot for orchestration run
      └── Events/
          ├── OrchestrationRunStartedEvent.cs
          ├── WorkflowTransitionEvent.cs
          └── SkillSelectedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── OrchestrationRunAlgorithms.fs
      ├── SelectSkillForPhase          # Select appropriate skill for phase
      ├── ExecuteWorkflowTransition    # Execute workflow transition
      ├── CheckWorkflowHookMilestone   # Check if workflow hook milestone is reached
      ├── ValidateSkillRequirements    # Validate skill requirements
      └── GetDefaultSkills             # Get default skills

Libr4.IDE.Application/
  └── OrchestrationRun/
      ├── Commands/
          ├── StartOrchestrationRunCommand.cs
          ├── ExecuteTransitionCommand.cs
      ├── Queries/
          ├── GetOrchestrationRunQuery.cs
      ├── DTOs/
          ├── SkillDto.cs
          ├── OrchestrationRunDto.cs
      ├── Handlers/
          ├── StartOrchestrationRunCommandHandler.cs
          ├── ExecuteTransitionCommandHandler.cs
      └── Validators/
          └── StartOrchestrationRunCommandValidator.cs

Libr4.IDE.Api/
  └── OrchestrationRunEndpoints.cs     # Minimal API endpoints
```

## Domain Model

### SkillType Enum

```csharp
public enum SkillType
{
    PlanRequest,        // Analyze and plan requests
    MultiFileEdit,      // Edit multiple files
    ValidationLoop,     // Run validation loops
    QAAutomation,       // QA automation
    CodeReview,         // Code review
    SecurityReview,     // Security review
    Testing,            // Testing
    Documentation       // Documentation
}
```

### Skill Value Object

```csharp
public class Skill
{
    public SkillType SkillType { get; }
    public string Name { get; }
    public string Description { get; }
    public List<string> Capabilities { get; }
    public Dictionary<string, object> Requirements { get; }
    public bool IsDefault { get; }
}
```

### WorkflowTransition Value Object

```csharp
public class WorkflowTransition
{
    public string FromState { get; }
    public string ToState { get; }
    public string TransitionType { get; }
    public Dictionary<string, object> TransitionData { get; }
    public DateTime TransitionedAt { get; }
}
```

### OrchestrationRun AggregateRoot

```csharp
public class OrchestrationRun : AggregateRoot<Guid>
{
    public string RunId { get; }
    public string TaskId { get; }
    public string CurrentState { get; }
    public Skill SelectedSkill { get; }
    public List<WorkflowTransition> Transitions { get; }
    public Dictionary<string, object> HookMilestones { get; }
    public string Status { get; }
    public DateTime StartedAt { get; }
    public DateTime? CompletedAt { get; }
}
```

## F# Algorithms

### SelectSkillForPhase

Selects appropriate skill for a phase:

```fsharp
let selectSkillForPhase (phaseId: string) (phaseName: string) (taskAnalysis: obj) : Skill =
    match phaseId.ToLower() with
    | "planning" -> Skills.planRequest
    | "database_model" | "backend_api" | "backend_service" -> Skills.multiFileEdit
    | "testing" -> Skills.qaAutomation
    | "validation" -> Skills.validationLoop
    | _ -> Skills.multiFileEdit
```

### ExecuteWorkflowTransition

Executes a workflow transition:

```fsharp
let executeWorkflowTransition (currentState: string) (targetState: string) (transitionData: Dictionary<string, obj>) : WorkflowTransition =
    let transitionType = determineTransitionType currentState targetState
    
    WorkflowTransition.Create(
        currentState,
        targetState,
        transitionType,
        transitionData,
        DateTime.UtcNow
    )
```

### CheckWorkflowHookMilestone

Checks if a workflow hook milestone is reached:

```fsharp
let checkWorkflowHookMilestone (milestone: string) (currentPhase: string) (completedPhases: string list) : bool =
    match milestone with
    | "after_planning" -> completedPhases |> List.contains "planning"
    | "after_backend" -> completedPhases |> List.contains "backend_api"
    | "after_frontend" -> completedPhases |> List.contains "frontend"
    | "after_testing" -> completedPhases |> List.contains "testing"
    | _ -> false
```

### GetDefaultSkills

Returns default skills:

```fsharp
let getDefaultSkills () : Skill list =
    [
        Skills.planRequest
        Skills.multiFileEdit
        Skills.validationLoop
        Skills.qaAutomation
    ]
```

## Application Layer (C#)

### Command Handler

```csharp
public class StartOrchestrationRunCommandHandler : IRequestHandler<StartOrchestrationRunCommand, OrchestrationRunDto>
{
    private readonly IOrchestrationRunAlgorithms _algorithms;
    
    public async Task<OrchestrationRunDto> Handle(StartOrchestrationRunCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var orchestrationRun = _algorithms.StartOrchestrationRun(
            request.TaskId,
            request.PhaseId,
            request.TaskAnalysis
        );
        
        // Map to DTO
        return orchestrationRun.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class OrchestrationRunEndpoints
{
    public static void MapOrchestrationRunEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/orchestration-run")
            .WithTags("Orchestration Run")
            .RequireAuthorization();
        
        group.MapPost("/start", async (
            StartOrchestrationRunCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("StartOrchestrationRun")
        .WithOpenApi();
    }
}
```

## Default Skills

| Skill Type | Name | Description | Capabilities |
|------------|------|-------------|--------------|
| PlanRequest | Plan Request | Analyze and plan user requests | analysis, decomposition, planning |
| MultiFileEdit | Multi-File Edit | Edit multiple files coherently | file_edit, consistency, validation |
| ValidationLoop | Validation Loop | Run validation loops with feedback | validation, testing, feedback |
| QAAutomation | QA Automation | Automated QA testing | testing, coverage, reporting |
| CodeReview | Code Review | Code review with guardrails | review, risk_assessment, recommendations |
| SecurityReview | Security Review | Security-focused review | security, vulnerability_scan, compliance |
| Testing | Testing | Comprehensive testing | unit_tests, integration_tests, e2e |
| Documentation | Documentation | Generate documentation | docs, comments, readme |

## Workflow Hook Milestones

- **after_planning**: Triggered after planning phase completes
- **after_database_model**: Triggered after database model phase completes
- **after_backend**: Triggered after backend phases complete
- **after_frontend**: Triggered after frontend phase completes
- **after_testing**: Triggered after testing phase completes
- **before_validation**: Triggered before validation phase starts
- **after_validation**: Triggered after validation phase completes

## Workflow States

- **idle**: Run not started
- **planning**: Planning phase
- **executing**: Executing a phase
- **validating**: Running validation
- **waiting**: Waiting for user input
- **blocked**: Blocked by dependency
- **completed**: Run completed successfully
- **failed**: Run failed

## Testing Strategy

1. **Unit Tests** - Test F# orchestration algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full orchestration flow

## Performance Considerations

- Orchestration is CPU-light (state transitions)
- Use caching for skill selection
- Implement streaming for long-running workflows

## Security Considerations

- Validate all skill selections
- Sanitize transition data
- Rate limit per user
- Audit all orchestration runs

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
