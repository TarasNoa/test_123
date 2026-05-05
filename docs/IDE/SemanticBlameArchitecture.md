# Semantic Blame Service - Architecture Documentation

## Overview

The Semantic Blame Service provides AI-powered git blame and code evolution analysis. It tracks code changes over time, identifies contributors, and provides semantic context for code modifications. Uses F# for git history analysis and evolution tracking algorithms.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Git history analysis, evolution tracking, blame algorithms (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── SemanticBlame/
      ├── BlameEntry.cs                 # Entity representing a blame entry
      ├── CodeEvolution.cs             # Entity representing code evolution
      ├── SemanticBlame.cs              # AggregateRoot for semantic blame
      └── Events/
          ├── BlameGeneratedEvent.cs
          ├── EvolutionAnalyzedEvent.cs
          └── BlameCompletedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── SemanticBlameAlgorithms.fs
      ├── AnalyzeGitHistory            # Analyze git history
      ├── TrackCodeEvolution           # Track code evolution
      ├── GenerateBlame                # Generate semantic blame
      ├── IdentifyContributors         # Identify code contributors
      └── RunSemanticBlame             # Run full semantic blame

Libr4.IDE.Application/
  └── SemanticBlame/
      ├── Commands/
          ├── RunBlameCommand.cs
      ├── Queries/
          ├── GetBlameQuery.cs
      ├── DTOs/
          ├── BlameEntryDto.cs
          /// CodeEvolutionDto.cs
          /// SemanticBlameDto.cs
      ├── Handlers/
          ├── RunBlameCommandHandler.cs
          /// GetBlameQueryHandler.cs
      └── Validators/
          └── RunBlameCommandValidator.cs

Libr4.IDE.Api/
  └── SemanticBlameEndpoints.cs        # Minimal API endpoints
```

## Domain Model

### BlameEntry Entity

```csharp
public class BlameEntry
{
    public Guid Id { get; }
    public string FilePath { get; }
    public int LineNumber { get; }
    public string Author { get; init; }
    public string CommitHash { get; }
    public DateTime CommitDate { get; }
    public string CommitMessage { get; }
}
```

### CodeEvolution Entity

```csharp
public class CodeEvolution
{
    public Guid Id { get; }
    public string FilePath { get; }
    public List<GitCommit> Commits { get; }
    public Dictionary<string, int> ContributorStats { get; }
}
```

### SemanticBlame AggregateRoot

```csharp
public class SemanticBlame : AggregateRoot<Guid>
{
    public string BlameId { get; }
    public string FilePath { get; }
    public List<BlameEntry> Entries { get; }
    public CodeEvolution Evolution { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### AnalyzeGitHistory

Analyzes git history:

```fsharp
let analyzeGitHistory (filePath: string) : GitCommit list =
    // Parse git log
    // Extract commits
    // Return commit history
```

### GenerateBlame

Generates semantic blame:

```fsharp
let generateBlame (filePath: string) : BlameEntry list =
    // Run git blame
    // Parse blame output
    // Return blame entries
```

## Application Layer (C#)

### Command Handler

```csharp
public class RunBlameCommandHandler : IRequestHandler<RunBlameCommand, SemanticBlameDto>
{
    private readonly ISemanticBlameAlgorithms _algorithms;
    
    public async Task<SemanticBlameDto> Handle(RunBlameCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var blame = _algorithms.RunSemanticBlame(
            request.FilePath,
            request.WorkspacePath
        );
        
        // Map to DTO
        return blame.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class SemanticBlameEndpoints
{
    public static void MapSemanticBlameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/semantic-blame")
            .WithTags("Semantic Blame")
            .RequireAuthorization();
        
        group.MapPost("/blame", async (
            RunBlameCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("RunSemanticBlame")
        .WithOpenApi();
    }
}
```

## Blame Information

| Field | Description | Example |
|-------|-------------|---------|
| Author | Commit author | "John Doe" |
| CommitHash | Git commit hash | "abc123def" |
| CommitDate | Date of commit | 2024-01-15 |
| CommitMessage | Commit message | "Fix bug in parser" |

## Testing Strategy

1. **Unit Tests** - Test F# git analysis algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full blame flow

## Performance Considerations

- Git operations can be slow for large repositories
- Use caching for blame results
- Implement background analysis for large files

## Security Considerations

- Validate all file paths before git operations
- Sanitize commit messages
- Rate limit per user
- Audit all blame operations

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
