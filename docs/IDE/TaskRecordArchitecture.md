# IDE Agent Task Record Service - Architecture Documentation

## Overview

The Task Record Service provides task persistence with override and resume state capabilities. It stores task execution history, checkpoints, and enables agents to resume interrupted tasks from saved state. Uses F# for state serialization and resume logic algorithms.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - State serialization, resume logic, checkpoint management (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── TaskRecord/
      ├── TaskState.cs                   # Enum (Pending, Running, Paused, Completed, Failed)
      ├── TaskCheckpoint.cs              # Entity representing a task checkpoint
      /// TaskRecord.cs                   # AggregateRoot for task record
      └── Events/
          ├── TaskCreatedEvent.cs
          ├── CheckpointCreatedEvent.cs
          └── TaskResumedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── TaskRecordAlgorithms.fs
      ├── CreateCheckpoint              # Create checkpoint for task state
      ├── SerializeState                # Serialize task state
      /// DeserializeState              # Deserialize task state
      /// ResumeTask                    # Resume task from checkpoint
      └── OverrideState                 # Override task state

Libr4.IDE.Application/
  └── TaskRecord/
      ├── Commands/
          ├── CreateTaskRecordCommand.cs
          ├── ResumeTaskCommand.cs
      ├── Queries/
          ├── GetTaskRecordQuery.cs
      ├── DTOs/
          ├── TaskCheckpointDto.cs
          /// TaskRecordDto.cs
      ├── Handlers/
          ├── CreateTaskRecordCommandHandler.cs
          /// ResumeTaskCommandHandler.cs
      └── Validators/
          └── CreateTaskRecordCommandValidator.cs

Libr4.IDE.Api/
  └── TaskRecordEndpoints.cs            # Minimal API endpoints
```

## Domain Model

### TaskState Enum

```csharp
public enum TaskState
{
    Pending,
    Running,
    Paused,
    Completed,
    Failed
}
```

### TaskCheckpoint Entity

```csharp
public class TaskCheckpoint
{
    public Guid Id { get; }
    public string CheckpointName { get; }
    public string SerializedState { get; }
    public DateTime CreatedAt { get; }
}
```

### TaskRecord AggregateRoot

```csharp
public class TaskRecord : AggregateRoot<Guid>
{
    public string RecordId { get; }
    public string TaskId { get; }
    public TaskState State { get; }
    public List<TaskCheckpoint> Checkpoints { get; }
    public string CurrentState { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### CreateCheckpoint

Creates checkpoint for task state:

```fsharp
let createCheckpoint (state: string) (name: string) : TaskCheckpoint =
    // Serialize state
    // Create checkpoint
    // Return checkpoint
```

### ResumeTask

Resumes task from checkpoint:

```fsharp
let resumeTask (checkpoint: TaskCheckpoint) : string option =
    // Deserialize state from checkpoint
    // Return restored state
```

## Application Layer (C#)

### Command Handler

```csharp
public class CreateTaskRecordCommandHandler : IRequestHandler<CreateTaskRecordCommand, TaskRecordDto>
{
    private readonly ITaskRecordAlgorithms _algorithms;
    
    public async Task<TaskRecordDto> Handle(CreateTaskRecordCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var record = _algorithms.CreateTaskRecord(
            request.TaskId,
            request.InitialState
        );
        
        // Map to DTO
        return record.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class TaskRecordEndpoints
{
    public static void MapTaskRecordEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/task-record")
            .WithTags("Task Record")
            .RequireAuthorization();
        
        group.MapPost("/create", async (
            CreateTaskRecordCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("CreateTaskRecord")
        .WithOpenApi();
    }
}
```

## Task States

| State | Description | Can Resume |
|-------|-------------|------------|
| Pending | Task is waiting to start | Yes |
| Running | Task is currently running | Yes |
| Paused | Task is paused | Yes |
| Completed | Task completed successfully | No |
| Failed | Task failed | Yes |

## Testing Strategy

1. **Unit Tests** - Test F# serialization and resume algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full task record and resume flow

## Performance Considerations

- State serialization can be CPU-intensive
- Use compression for large states
- Implement background checkpointing
- Cache frequently accessed states

## Security Considerations

- Validate all state data before serialization
- Sanitize state before storage
- Rate limit per user
- Audit all task record operations

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
