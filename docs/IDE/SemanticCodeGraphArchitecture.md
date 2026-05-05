# Semantic Code Graph Service - Architecture Documentation

## Overview

The Semantic Code Graph Service generates embeddings for code entities and relationships, enabling semantic search and graph-based code navigation. It uses F# for graph algorithms and vector operations, providing a powerful tool for understanding code structure and finding related code.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Graph algorithms, embeddings, vector search (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── SemanticCodeGraph/
      ├── CodeEntity.cs                # Entity representing a code entity
      ├── CodeRelationship.cs          # Entity representing relationships between entities
      ├── SemanticGraph.cs             # AggregateRoot for semantic graph
      └── Events/
          ├── GraphCreatedEvent.cs
          ├── EntityAddedEvent.cs
          └── RelationshipAddedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── SemanticCodeGraphAlgorithms.fs
      ├── GenerateEmbeddings           # Generate embeddings for code
      ├── BuildGraph                   # Build semantic graph from code
      ├── VectorSearch                 # Perform vector similarity search
      ├── FindRelatedCode             # Find related code entities
      ├── UpdateGraph                  # Update graph with new code
      └── CompressGraph               # Compress graph for storage

Libr4.IDE.Application/
  └── SemanticCodeGraph/
      ├── Commands/
          ├── BuildGraphCommand.cs
          ├── SearchCodeCommand.cs
      ├── Queries/
          ├── GetGraphQuery.cs
      ├── DTOs/
          ├── CodeEntityDto.cs
          ├── CodeRelationshipDto.cs
          ├── SemanticGraphDto.cs
      ├── Handlers/
          ├── BuildGraphCommandHandler.cs
          ├── SearchCodeCommandHandler.cs
      └── Validators/
          └── BuildGraphCommandValidator.cs

Libr4.IDE.Api/
  └── SemanticCodeGraphEndpoints.cs     # Minimal API endpoints
```

## Domain Model

### CodeEntity Entity

```csharp
public class CodeEntity
{
    public Guid Id { get; }
    public string EntityType { get; }
    public string Name { get; }
    public string FilePath { get; }
    public float[] Embedding { get; }
    public Dictionary<string, object> Metadata { get; }
}
```

### CodeRelationship Entity

```csharp
public class CodeRelationship
{
    public Guid Id { get; }
    public Guid SourceEntityId { get; }
    public Guid TargetEntityId { get; }
    public string RelationshipType { get; }
    public float Weight { get; }
}
```

### SemanticGraph AggregateRoot

```csharp
public class SemanticGraph : AggregateRoot<Guid>
{
    public string GraphId { get; }
    public string WorkspaceId { get; }
    public List<CodeEntity> Entities { get; }
    public List<CodeRelationship> Relationships { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### GenerateEmbeddings

Generates embeddings for code:

```fsharp
let generateEmbeddings (code: string) (entityType: string) : float[] =
    // In real implementation, would use ML.NET or external embedding service
    // Simplified: generate hash-based embedding
    let hash = code.GetHashCode() |> abs
    [| for i in 0..127 -> (hash + i) % 100 |> float |> (*) 0.01 |]
```

### BuildGraph

Builds semantic graph from code:

```fsharp
let buildGraph (files: (string * string) list) : (CodeEntity list * CodeRelationship list) =
    // Parse code entities from files
    // Generate embeddings for each entity
    // Identify relationships between entities
    // Return entities and relationships
```

### VectorSearch

Performs vector similarity search:

```fsharp
let vectorSearch (queryEmbedding: float[]) (entities: CodeEntity list) (topK: int) : (CodeEntity * float) list =
    entities
    |> List.map (fun entity ->
        let similarity = cosineSimilarity queryEmbedding entity.Embedding
        (entity, similarity)
    )
    |> List.sortByDescending snd
    |> List.take topK
```

## Application Layer (C#)

### Command Handler

```csharp
public class BuildGraphCommandHandler : IRequestHandler<BuildGraphCommand, SemanticGraphDto>
{
    private readonly ISemanticCodeGraphAlgorithms _algorithms;
    
    public async Task<SemanticGraphDto> Handle(BuildGraphCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var semanticGraph = _algorithms.BuildGraph(
            request.WorkspaceId,
            request.Files
        );
        
        // Map to DTO
        return semanticGraph.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class SemanticCodeGraphEndpoints
{
    public static void MapSemanticCodeGraphEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/semantic-graph")
            .WithTags("Semantic Code Graph")
            .RequireAuthorization();
        
        group.MapPost("/build-graph", async (
            BuildGraphCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("BuildSemanticGraph")
        .WithOpenApi();
    }
}
```

## Entity Types

| Entity Type | Description | Example |
|-------------|-------------|---------|
| Class | Class definition | `User`, `Product` |
| Function | Function/method | `getUser`, `createOrder` |
| Variable | Variable declaration | `userId`, `orderItems` |
| Module | Module/package | `user_service`, `models` |
| Interface | Interface definition | `IRepository`, `IValidator` |

## Relationship Types

| Relationship Type | Description | Example |
|-------------------|-------------|---------|
| Calls | Function/method call | `main` calls `processOrder` |
| Inherits | Class inheritance | `AdminUser` inherits `User` |
| Implements | Interface implementation | `UserRepository` implements `IRepository` |
| Uses | Dependency usage | `UserService` uses `UserRepository` |
| References | Reference/usage | `Order` references `Product` |

## Testing Strategy

1. **Unit Tests** - Test F# graph algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full graph building and search flow

## Performance Considerations

- Embedding generation can be CPU-intensive
- Use caching for embeddings
- Implement approximate nearest neighbor search for large graphs
- Background processing for large codebases

## Security Considerations

- Validate all file paths to prevent directory traversal
- Sanitize code content before processing
- Rate limit per user
- Audit all graph operations

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
