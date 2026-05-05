# Shadow Workspace Service - Architecture Documentation

## Overview

The Shadow Workspace Service provides a virtual file system for safe agent operations, including collaborative multi-user staging, preview, and approval workflows. It performs various validations like AST validation, import smoke tests, pytest execution, and frontend type checking.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Validation algorithms, AST parsing, test execution (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── ShadowWorkspace/
      ├── ValidationType.cs             # Enum (AST, ImportSmoke, Pytest, Typecheck)
      ├── ValidationResult.cs           # Value object for validation result
      ├── ShadowFile.cs                 # Entity representing a file in shadow workspace
      ├── ShadowWorkspace.cs             # AggregateRoot for shadow workspace
      └── Events/
          ├── WorkspaceCreatedEvent.cs
          ├── ValidationCompletedEvent.cs
          └── FileAddedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── ShadowWorkspaceAlgorithms.fs
      ├── RunASTValidation             # Run AST validation
      ├── RunImportSmokeTest           # Run import smoke tests
      ├── RunPytestExecution           # Run pytest execution
      ├── RunTypecheck                 # Run frontend type checking
      ├── ValidateFile                 # Validate a single file
      ├── AggregateValidationResults     # Aggregate validation results
      └── CreateShadowWorkspace         # Create shadow workspace

Libr4.IDE.Application/
  └── ShadowWorkspace/
      ├── Commands/
          ├── CreateShadowWorkspaceCommand.cs
          ├── RunValidationCommand.cs
      ├── Queries/
          ├── GetShadowWorkspaceQuery.cs
      ├── DTOs/
          ├── ValidationResultDto.cs
          ├── ShadowFileDto.cs
          ├── ShadowWorkspaceDto.cs
      ├── Handlers/
          ├── CreateShadowWorkspaceCommandHandler.cs
          ├── RunValidationCommandHandler.cs
      └── Validators/
          └── CreateShadowWorkspaceCommandValidator.cs

Libr4.IDE.Api/
  └── ShadowWorkspaceEndpoints.cs        # Minimal API endpoints
```

## Domain Model

### ValidationType Enum

```csharp
public enum ValidationType
{
    AST,           // AST validation
    ImportSmoke,   // Import smoke tests
    Pytest,        // Pytest execution
    Typecheck      // Frontend type checking
}
```

### ValidationResult Value Object

```csharp
public class ValidationResult
{
    public ValidationType Type { get; }
    public bool Passed { get; }
    public List<string> Errors { get; }
    public List<string> Warnings { get; }
    public TimeSpan Duration { get; }
}
```

### ShadowFile Entity

```csharp
public class ShadowFile
{
    public Guid Id { get; }
    public string FilePath { get; }
    public string Content { get; }
    public string Status { get; }
    public List<ValidationResult> ValidationResults { get; }
}
```

### ShadowWorkspace AggregateRoot

```csharp
public class ShadowWorkspace : AggregateRoot<Guid>
{
    public string WorkspaceId { get; }
    public string ParentWorkspaceId { get; }
    public List<ShadowFile> Files { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### RunASTValidation

Runs AST validation:

```fsharp
let runASTValidation (file: ShadowFile) : ValidationResult =
    // Parse file content into AST
    // Validate AST structure
    // Check for syntax errors
    // Return validation result
```

### RunImportSmokeTest

Runs import smoke tests:

```fsharp
let runImportSmokeTest (workspace: ShadowWorkspace) : ValidationResult =
    // Check all imports resolve
    // Validate import paths
    // Check for circular imports
    // Return validation result
```

### RunPytestExecution

Runs pytest execution:

```fsharp
let runPytestExecution (workspace: ShadowWorkspace) : ValidationResult =
    // Discover and run pytest tests
    // Collect test results
    // Check coverage
    // Return validation result
```

### RunTypecheck

Runs frontend type checking:

```fsharp
let runTypecheck (workspace: ShadowWorkspace) : ValidationResult =
    // Run TypeScript type checking
    // Check for type errors
    // Validate type definitions
    // Return validation result
```

## Application Layer (C#)

### Command Handler

```csharp
public class CreateShadowWorkspaceCommandHandler : IRequestHandler<CreateShadowWorkspaceCommand, ShadowWorkspaceDto>
{
    private readonly IShadowWorkspaceAlgorithms _algorithms;
    
    public async Task<ShadowWorkspaceDto> Handle(CreateShadowWorkspaceCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var shadowWorkspace = _algorithms.CreateShadowWorkspace(
            request.ParentWorkspaceId,
            request.Files
        );
        
        // Map to DTO
        return shadowWorkspace.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class ShadowWorkspaceEndpoints
{
    public static void MapShadowWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/shadow-workspace")
            .WithTags("Shadow Workspace")
            .RequireAuthorization();
        
        group.MapPost("/create", async (
            CreateShadowWorkspaceCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("CreateShadowWorkspace")
        .WithOpenApi();
    }
}
```

## Validation Types

| Validation Type | Description | Target | Output |
|-----------------|-------------|--------|--------|
| AST | AST validation | Python files | Syntax errors, structure issues |
| ImportSmoke | Import smoke tests | Python workspace | Unresolved imports, circular dependencies |
| Pytest | Test execution | Python workspace | Test results, coverage |
| Typecheck | Type checking | TypeScript files | Type errors, type definition issues |

## Testing Strategy

1. **Unit Tests** - Test F# validation algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full shadow workspace flow

## Performance Considerations

- Validation can be CPU-intensive (AST parsing, type checking)
- Use caching for validation results
- Implement parallel validation where possible
- Background processing for large workspaces

## Security Considerations

- Validate all file paths to prevent directory traversal
- Sanitize file content before processing
- Rate limit per user
- Audit all shadow workspace operations

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
