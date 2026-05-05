# Persistent Memory System for AI Agents - Architecture Documentation

## Overview

The Persistent Memory System provides AI agents with long-term memory capabilities, storing memory fragments, compressing them for efficiency, and enabling retrieval based on relevance. It uses F# for memory compression algorithms and relevance scoring.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Memory compression, relevance scoring, retrieval algorithms (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── AgentMemorySystem/
      ├── MemoryFragment.cs            # Entity representing a memory fragment
      ├── MemoryCompressionLevel.cs    # Enum (None, Low, Medium, High)
      ├── AgentMemory.cs                # AggregateRoot for agent memory
      └── Events/
          ├── MemoryCreatedEvent.cs
          ├── FragmentAddedEvent.cs
          └── MemoryCompressedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── AgentMemorySystemAlgorithms.fs
      ├── CompressMemoryFragment      # Compress memory fragment
      ├── CalculateRelevanceScore     # Calculate relevance score
      ├── RetrieveRelevantMemories    # Retrieve relevant memories
      ├── MergeMemoryFragments         # Merge similar fragments
      ├── ExpireOldMemories           # Expire old memory fragments
      └── CreateAgentMemory           # Create agent memory

Libr4.IDE.Application/
  └── AgentMemorySystem/
      ├── Commands/
          ├── CreateMemoryCommand.cs
          ├── AddFragmentCommand.cs
      ├── Queries/
          ├── GetMemoryQuery.cs
      ├── DTOs/
          ├── MemoryFragmentDto.cs
          ├── AgentMemoryDto.cs
      ├── Handlers/
          ├── CreateMemoryCommandHandler.cs
          ├── AddFragmentCommandHandler.cs
      └── Validators/
          └── CreateMemoryCommandValidator.cs

Libr4.IDE.Api/
  └── AgentMemorySystemEndpoints.cs      # Minimal API endpoints
```

## Domain Model

### MemoryCompressionLevel Enum

```csharp
public enum MemoryCompressionLevel
{
    None,
    Low,
    Medium,
    High
}
```

### MemoryFragment Entity

```csharp
public class MemoryFragment
{
    public Guid Id { get; }
    public string Content { get; }
    public Dictionary<string, object> Metadata { get; }
    public float RelevanceScore { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ExpiresAt { get; }
}
```

### AgentMemory AggregateRoot

```csharp
public class AgentMemory : AggregateRoot<Guid>
{
    public string MemoryId { get; }
    public string AgentId { get; }
    public List<MemoryFragment> Fragments { get; }
    public MemoryCompressionLevel CompressionLevel { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### CompressMemoryFragment

Compresses memory fragment:

```fsharp
let compressMemoryFragment (fragment: MemoryFragment) (level: MemoryCompressionLevel) : MemoryFragment =
    match level with
    | None -> fragment
    | Low -> compressBasic fragment
    | Medium -> compressAdvanced fragment
    | High -> compressAggressive fragment
```

### CalculateRelevanceScore

Calculates relevance score for memory:

```fsharp
let calculateRelevanceScore (query: string) (fragment: MemoryFragment) : float =
    // Calculate similarity between query and fragment
    // Use keyword matching, embeddings, etc.
```

### RetrieveRelevantMemories

Retrieves relevant memories based on query:

```fsharp
let retrieveRelevantMemories (query: string) (fragments: MemoryFragment list) (topK: int) : MemoryFragment list =
    fragments
    |> List.map (fun frag -> (frag, calculateRelevanceScore query frag))
    |> List.sortByDescending snd
    |> List.take topK
    |> List.map fst
```

## Application Layer (C#)

### Command Handler

```csharp
public class CreateMemoryCommandHandler : IRequestHandler<CreateMemoryCommand, AgentMemoryDto>
{
    private readonly IAgentMemorySystemAlgorithms _algorithms;
    
    public async Task<AgentMemoryDto> Handle(CreateMemoryCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var agentMemory = _algorithms.CreateAgentMemory(
            request.AgentId,
            request.CompressionLevel
        );
        
        // Map to DTO
        return agentMemory.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class AgentMemorySystemEndpoints
{
    public static void MapAgentMemorySystemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/agent-memory")
            .WithTags("Agent Memory System")
            .RequireAuthorization();
        
        group.MapPost("/create-memory", async (
            CreateMemoryCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("CreateAgentMemory")
        .WithOpenApi();
    }
}
```

## Compression Levels

| Compression Level | Description | Storage Savings | Quality Loss |
|-------------------|-------------|-----------------|--------------|
| None | No compression | 0% | None |
| Low | Basic compression | 25% | Minimal |
| Medium | Advanced compression | 50% | Moderate |
| High | Aggressive compression | 75% | Significant |

## Testing Strategy

1. **Unit Tests** - Test F# memory algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full memory system flow

## Performance Considerations

- Memory compression can be CPU-intensive
- Use caching for frequently accessed memories
- Implement background compression for large memory stores
- Use approximate similarity search for large memory sets

## Security Considerations

- Validate all memory content to prevent injection
- Sanitize metadata before storage
- Rate limit per user
- Audit all memory operations

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
