# AI Workflow Automation System - Architecture Documentation

## Overview

The AI Workflow Automation System automatically distills workflows into reusable skills. It analyzes completed workflows, extracts patterns, and creates new skills that can be reused for similar tasks. Uses F# for pattern recognition and skill extraction algorithms.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Pattern recognition, skill extraction, workflow analysis (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── AIWorkflowAutomation/
      ├── WorkflowPattern.cs            # Entity representing a workflow pattern
      ├── ExtractedSkill.cs             # Entity representing an extracted skill
      ├── WorkflowAnalysis.cs            # AggregateRoot for workflow analysis
      └── Events/
          ├── AnalysisStartedEvent.cs
          ├── SkillExtractedEvent.cs
          └── PatternDetectedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── AIWorkflowAutomationAlgorithms.fs
      ├── AnalyzeWorkflow              # Analyze workflow for patterns
      ├── ExtractSkill                  # Extract skill from workflow
      ├── DetectPatterns                # Detect recurring patterns
      ├── ValidateSkill                 # Validate extracted skill
      └── DistillWorkflow               # Distill workflow into skill

Libr4.IDE.Application/
  └── AIWorkflowAutomation/
      ├── Commands/
          ├── DistillWorkflowCommand.cs
      ├── Queries/
          ├── GetExtractedSkillsQuery.cs
      ├── DTOs/
          ├── WorkflowPatternDto.cs
          ├── ExtractedSkillDto.cs
          ├── WorkflowAnalysisDto.cs
      ├── Handlers/
          ├── DistillWorkflowCommandHandler.cs
          ├── GetExtractedSkillsQueryHandler.cs
      └── Validators/
          └── DistillWorkflowCommandValidator.cs

Libr4.IDE.Api/
  └── AIWorkflowAutomationEndpoints.cs      # Minimal API endpoints
```

## Domain Model

### WorkflowPattern Entity

```csharp
public class WorkflowPattern
{
    public Guid Id { get; }
    public string PatternName { get; }
    public string Description { get; }
    public List<string> Steps { get; }
    public int Frequency { get; }
    public Dictionary<string, object> Metadata { get; }
}
```

### ExtractedSkill Entity

```csharp
public class ExtractedSkill
{
    public Guid Id { get; }
    public string SkillName { get; }
    public string Description { get; }
    public List<string> Capabilities { get; }
    public double ConfidenceScore { get; }
}
```

### WorkflowAnalysis AggregateRoot

```csharp
public class WorkflowAnalysis : AggregateRoot<Guid>
{
    public string AnalysisId { get; }
    public string WorkflowId { get; }
    public List<WorkflowPattern> Patterns { get; }
    public List<ExtractedSkill> ExtractedSkills { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### AnalyzeWorkflow

Analyzes workflow for patterns:

```fsharp
let analyzeWorkflow (workflow: string) : WorkflowPattern list =
    // Parse workflow steps
    // Identify recurring patterns
    // Return detected patterns
```

### ExtractSkill

Extracts skill from workflow:

```fsharp
let extractSkill (pattern: WorkflowPattern) : ExtractedSkill option =
    // Validate pattern can be distilled
    // Create skill definition
    // Return extracted skill
```

## Application Layer (C#)

### Command Handler

```csharp
public class DistillWorkflowCommandHandler : IRequestHandler<DistillWorkflowCommand, WorkflowAnalysisDto>
{
    private readonly IAIWorkflowAutomationAlgorithms _algorithms;
    
    public async Task<WorkflowAnalysisDto> Handle(DistillWorkflowCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var analysis = _algorithms.DistillWorkflow(
            request.WorkflowId,
            request.WorkflowSteps
        );
        
        // Map to DTO
        return analysis.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class AIWorkflowAutomationEndpoints
{
    public static void MapAIWorkflowAutomationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/workflow-automation")
            .WithTags("AI Workflow Automation")
            .RequireAuthorization();
        
        group.MapPost("/distill", async (
            DistillWorkflowCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("DistillWorkflow")
        .WithOpenApi();
    }
}
```

## Skill Extraction Strategy

1. Analyze completed workflows
2. Identify recurring patterns
3. Validate patterns can be distilled
4. Create skill definitions
5. Calculate confidence scores
6. Store extracted skills

## Testing Strategy

1. **Unit Tests** - Test F# pattern recognition algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full workflow automation flow

## Performance Considerations

- Pattern analysis can be CPU-intensive
- Use caching for pattern detection
- Implement background analysis for large workflows

## Security Considerations

- Validate all workflow steps before analysis
- Sanitize skill names and descriptions
- Rate limit per user
- Audit all skill extractions

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
