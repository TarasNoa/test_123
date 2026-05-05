# Architectural Guardrails Service - Architecture Documentation

## Overview

The Architectural Guardrails Service provides AST-based rule validation and architecture validation. It enforces architectural patterns, detects violations, and provides feedback to maintain code quality. Uses F# for AST parsing and rule validation algorithms.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - AST parsing, rule validation, architecture checking (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── ArchitecturalGuardrails/
      ├── RuleType.cs                    # Enum (Naming, Structure, Dependency, Security)
      ├── GuardrailRule.cs               # Entity representing a guardrail rule
      /// GuardrailViolation.cs          # Value object for rule violation
      /// ArchitectureValidation.cs      # AggregateRoot for architecture validation
      └── Events/
          ├── ValidationStartedEvent.cs
          ├── ViolationDetectedEvent.cs
          └── ValidationCompletedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── ArchitecturalGuardrailsAlgorithms.fs
      /// ParseAST                      # Parse AST from code
      /// ValidateRules                 # Validate code against rules
      /// CheckArchitecture             # Check architectural patterns
      /// DetectViolations              # Detect rule violations
      └── RunValidation                # Run full validation

Libr4.IDE.Application/
  └── ArchitecturalGuardrails/
      ├── Commands/
          ├── RunValidationCommand.cs
      ├── Queries/
          ├── GetViolationsQuery.cs
      ├── DTOs/
          ├── GuardrailRuleDto.cs
          /// GuardrailViolationDto.cs
          /// ArchitectureValidationDto.cs
      ├── Handlers/
          ├── RunValidationCommandHandler.cs
          /// GetViolationsQueryHandler.cs
      └── Validators/
          └── RunValidationCommandValidator.cs

Libr4.IDE.Api/
  └── ArchitecturalGuardrailsEndpoints.cs  # Minimal API endpoints
```

## Domain Model

### RuleType Enum

```csharp
public enum RuleType
{
    Naming,
    Structure,
    Dependency,
    Security
}
```

### GuardrailRule Entity

```csharp
public class GuardrailRule
{
    public Guid Id { get; }
    public string RuleName { get; }
    public RuleType Type { get; }
    public string Pattern { get; }
    public string Description { get; }
    public bool IsActive { get; }
}
```

### GuardrailViolation Value Object

```csharp
public class GuardrailViolation
{
    public Guid Id { get; }
    public GuardrailRule Rule { get; }
    public string FilePath { get; }
    public int LineNumber { get; }
    public string Message { get; }
    public Severity Severity { get; }
}
```

### ArchitectureValidation AggregateRoot

```csharp
public class ArchitectureValidation : AggregateRoot<Guid>
{
    public string ValidationId { get; }
    public string WorkspaceId { get; }
    public List<GuardrailRule> Rules { get; }
    public List<GuardrailViolation> Violations { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### ValidateRules

Validates code against rules:

```fsharp
let validateRules (code: string) (rules: GuardrailRule list) : GuardrailViolation list =
    // Parse code
    // Check each rule
    // Return violations
```

### CheckArchitecture

Checks architectural patterns:

```fsharp
let checkArchitecture (files: (string * string) list) : bool =
    // Check architectural patterns
    // Return validation result
```

## Application Layer (C#)

### Command Handler

```csharp
public class RunValidationCommandHandler : IRequestHandler<RunValidationCommand, ArchitectureValidationDto>
{
    private readonly IArchitecturalGuardrailsAlgorithms _algorithms;
    
    public async Task<ArchitectureValidationDto> Handle(RunValidationCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var validation = _algorithms.RunValidation(
            request.WorkspaceId,
            request.Files,
            request.Rules
        );
        
        // Map to DTO
        return validation.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class ArchitecturalGuardrailsEndpoints
{
    public static void MapArchitecturalGuardrailsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/architectural-guardrails")
            .WithTags("Architectural Guardrails")
            .RequireAuthorization();
        
        group.MapPost("/validate", async (
            RunValidationCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("RunValidation")
        .WithOpenApi();
    }
}
```

## Rule Types

| Rule Type | Description | Example |
|-----------|-------------|---------|
| Naming | Enforce naming conventions | PascalCase for classes |
| Structure | Enforce code structure | File organization |
| Dependency | Enforce dependency rules | No circular dependencies |
| Security | Enforce security patterns | No hardcoded secrets |

## Testing Strategy

1. **Unit Tests** - Test F# validation algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full validation flow

## Performance Considerations

- AST parsing can be CPU-intensive
- Use caching for parsed ASTs
- Implement background validation for large codebases

## Security Considerations

- Validate all file paths before parsing
- Sanitize code content
- Rate limit per user
- Audit all validation operations

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
