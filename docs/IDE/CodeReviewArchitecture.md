# IDE Code Review Service - Architecture Documentation

## Overview

The Code Review Service performs automated code reviews, applying architectural guardrails, detecting risks (e.g., sensitive surfaces, contract drift), and suggesting recommendations. It leverages the Orchestration Run Service to resolve skill selection for reviews.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Review algorithms, risk detection, architectural guardrails (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── CodeReview/
      ├── ReviewType.cs                # Enum (Architecture, Security, Performance, Quality)
      ├── RiskSeverity.cs              # Enum (Low, Medium, High, Critical)
      ├── ReviewIssue.cs                # Value object for review issues
      ├── CodeReview.cs                # AggregateRoot for code review
      └── Events/
          ├── CodeReviewStartedEvent.cs
          ├── ReviewCompletedEvent.cs
          └── IssueDetectedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── CodeReviewAlgorithms.fs
      ├── RunArchitectureReview       # Run architectural review
      ├── RunSecurityReview           # Run security review
      ├── RunPerformanceReview        # Run performance review
      ├── DetectRisks                 # Detect risks in code
      ├── ApplyArchitecturalGuardrails # Apply architectural guardrails
      ├── GenerateRecommendations      # Generate recommendations
      └── RunCodeReview               # Main review orchestration

Libr4.IDE.Application/
  └── CodeReview/
      ├── Commands/
          ├── RunCodeReviewCommand.cs
      ├── Queries/
          ├── GetCodeReviewQuery.cs
      ├── DTOs/
          ├── ReviewIssueDto.cs
          ├── CodeReviewDto.cs
      ├── Handlers/
          ├── RunCodeReviewCommandHandler.cs
          ├── GetCodeReviewQueryHandler.cs
      └── Validators/
          └── RunCodeReviewCommandValidator.cs

Libr4.IDE.Api/
  └── CodeReviewEndpoints.cs           # Minimal API endpoints
```

## Domain Model

### ReviewType Enum

```csharp
public enum ReviewType
{
    Architecture,  // Architectural review
    Security,      // Security review
    Performance,   // Performance review
    Quality        // Code quality review
}
```

### RiskSeverity Enum

```csharp
public enum RiskSeverity
{
    Low,
    Medium,
    High,
    Critical
}
```

### ReviewIssue Value Object

```csharp
public class ReviewIssue
{
    public ReviewType Type { get; }
    public string Description { get; }
    public RiskSeverity Severity { get; }
    public string FilePath { get; }
    public int? LineNumber { get; }
    public string Recommendation { get; }
}
```

### CodeReview AggregateRoot

```csharp
public class CodeReview : AggregateRoot<Guid>
{
    public string ReviewId { get; }
    public string WorkspaceId { get; }
    public List<string> Files { get; }
    public List<ReviewIssue> Issues { get; }
    public string Status { get; }
    public DateTime StartedAt { get; }
    public DateTime? CompletedAt { get; }
}
```

## F# Algorithms

### RunArchitectureReview

Runs architectural review:

```fsharp
let runArchitectureReview (files: string list) : ReviewIssue list =
    // Check architectural patterns
    // Validate layer separation
    // Check for anti-patterns
    // Return architectural issues
```

### RunSecurityReview

Runs security review:

```fsharp
let runSecurityReview (files: string list) : ReviewIssue list =
    // Check for security vulnerabilities
    // Validate sensitive data handling
    // Check for hardcoded secrets
    // Return security issues
```

### RunPerformanceReview

Runs performance review:

```fsharp
let runPerformanceReview (files: string list) : ReviewIssue list =
    // Check for performance issues
    // Validate algorithmic complexity
    // Check for N+1 queries
    // Return performance issues
```

### DetectRisks

Detects risks in code:

```fsharp
let detectRisks (content: string) (filePath: string) : ReviewIssue list =
    // Detect sensitive surfaces
    // Detect contract drift
    // Detect other risks
    // Return risk issues
```

## Application Layer (C#)

### Command Handler

```csharp
public class RunCodeReviewCommandHandler : IRequestHandler<RunCodeReviewCommand, CodeReviewDto>
{
    private readonly ICodeReviewAlgorithms _algorithms;
    
    public async Task<CodeReviewDto> Handle(RunCodeReviewCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var codeReview = _algorithms.RunCodeReview(
            request.WorkspaceId,
            request.Files,
            request.ReviewTypes
        );
        
        // Map to DTO
        return codeReview.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class CodeReviewEndpoints
{
    public static void MapCodeReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/code-review")
            .WithTags("Code Review")
            .RequireAuthorization();
        
        group.MapPost("/run-review", async (
            RunCodeReviewCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("RunCodeReview")
        .WithOpenApi();
    }
}
```

## Review Types

| Review Type | Description | Checks | Output |
|-------------|-------------|--------|--------|
| Architecture | Architectural review | Layer separation, patterns, anti-patterns | Architectural issues, recommendations |
| Security | Security review | Vulnerabilities, secrets, sensitive data | Security issues, risk levels |
| Performance | Performance review | Complexity, N+1 queries, optimization | Performance issues, recommendations |
| Quality | Code quality review | Style, maintainability, test coverage | Quality issues, recommendations |

## Risk Levels

| Risk Severity | Description | Action Required |
|---------------|-------------|-----------------|
| Low | Minor issues | Optional fix |
| Medium | Moderate issues | Recommended fix |
| High | Significant issues | Required fix |
| Critical | Critical issues | Immediate fix |

## Testing Strategy

1. **Unit Tests** - Test F# review algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full code review flow

## Performance Considerations

- Code review can be CPU-intensive (AST parsing, analysis)
- Use caching for review results
- Implement parallel review where possible
- Background processing for large codebases

## Security Considerations

- Validate all file paths to prevent directory traversal
- Sanitize file content before analysis
- Rate limit per user
- Audit all code review operations

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
