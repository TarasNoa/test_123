# Code Intelligence Service - Architecture Documentation

## Overview

The Code Intelligence Service provides LSP (Language Server Protocol) integration and smart code completions. It offers context-aware suggestions, code navigation, and semantic analysis. Uses F# for completion ranking and context analysis algorithms.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation, LSP client (Infrastructure)
- **F#** - Completion ranking, context analysis, suggestion algorithms (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── CodeIntelligence/
      ├── CompletionType.cs              # Enum (Keyword, Variable, Function, Type)
      /// CodeSuggestion.cs               # Entity representing a code suggestion
      /// CompletionContext.cs            # Value object for completion context
      /// CodeIntelligence.cs             # AggregateRoot for code intelligence
      └── Events/
          ├── CompletionRequestedEvent.cs
          ├── SuggestionsGeneratedEvent.cs
          └── CompletionCompletedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── CodeIntelligenceAlgorithms.fs
      /// AnalyzeContext                # Analyze code context
      /// RankCompletions               # Rank completion suggestions
      /// GenerateSuggestions            # Generate code suggestions
      /// GetCompletions                 # Get completions for position
      └── RunCompletion                # Run full completion

Libr4.IDE.Application/
  └── CodeIntelligence/
      ├── Commands/
          ├── GetCompletionsCommand.cs
      ├── Queries/
          /// GetCompletionHistoryQuery.cs
      ├── DTOs/
          /// CodeSuggestionDto.cs
          /// CompletionContextDto.cs
          /// CodeIntelligenceDto.cs
      ├── Handlers/
          ├── GetCompletionsCommandHandler.cs
      └── Validators/
          └── GetCompletionsCommandValidator.cs

Libr4.IDE.Api/
  └── CodeIntelligenceEndpoints.cs      # Minimal API endpoints
```

## Domain Model

### CompletionType Enum

```csharp
public enum CompletionType
{
    Keyword,
    Variable,
    Function,
    Type,
    Property,
    Method
}
```

### CodeSuggestion Entity

```csharp
public class CodeSuggestion
{
    public Guid Id { get; }
    public string SuggestionText { get; }
    public CompletionType Type { get; }
    public double RelevanceScore { get; }
    public string Description { get; }
}
```

### CompletionContext Value Object

```csharp
public class CompletionContext
{
    public string FilePath { get; }
    public int Line { get; }
    public int Column { get; }
    public string Prefix { get; }
    public string SurroundingCode { get; }
}
```

### CodeIntelligence AggregateRoot

```csharp
public class CodeIntelligence : AggregateRoot<Guid>
{
    public string SessionId { get; }
    public CompletionContext Context { get; }
    public List<CodeSuggestion> Suggestions { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### AnalyzeContext

Analyzes code context:

```fsharp
let analyzeContext (code: string) (position: int) : CompletionContext =
    // Parse code structure
    // Determine language context
    // Return context
```

### RankCompletions

Ranks completion suggestions:

```fsharp
let rankCompletions (suggestions: CodeSuggestion list) (context: CompletionContext) : CodeSuggestion list =
    // Score each suggestion
    // Sort by relevance
    // Return ranked suggestions
```

## Application Layer (C#)

### Command Handler

```csharp
public class GetCompletionsCommandHandler : IRequestHandler<GetCompletionsCommand, CodeIntelligenceDto>
{
    private readonly ICodeIntelligenceAlgorithms _algorithms;
    
    public async Task<CodeIntelligenceDto> Handle(GetCompletionsCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var completions = _algorithms.GetCompletions(
            request.FilePath,
            request.Line,
            request.Column,
            request.Code
        );
        
        // Map to DTO
        return completions.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class CodeIntelligenceEndpoints
{
    public static void MapCodeIntelligenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/code-intelligence")
            .WithTags("Code Intelligence")
            .RequireAuthorization();
        
        group.MapPost("/completions", async (
            GetCompletionsCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("GetCompletions")
        .WithOpenApi();
    }
}
```

## Completion Types

| Type | Description | Example |
|------|-------------|---------|
| Keyword | Language keywords | `if`, `for`, `while` |
| Variable | Variable names | `myVariable` |
| Function | Function names | `calculateTotal` |
| Type | Type names | `string`, `List<T>` |
| Property | Property names | `User.Name` |
| Method | Method names | `object.toString()` |

## Testing Strategy

1. **Unit Tests** - Test F# ranking algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full completion flow

## Performance Considerations

- Completion must be fast (<100ms)
- Use caching for common completions
- Implement incremental analysis
- Debounce rapid requests

## Security Considerations

- Validate all file paths
- Sanitize code content
- Rate limit per user
- Audit all completion requests

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
