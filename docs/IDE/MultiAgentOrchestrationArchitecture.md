# Multi-Agent Orchestration Service - Architecture Documentation

## Overview

The Multi-Agent Orchestration Service coordinates multiple specialized AI agents. It defines AgentRole (EXECUTOR, LINTER, SECURITY, REVIEWER, TESTER, ARCHITECT, DEBUGGER), manages AgentInstance with performance scores, handles OrchestrationTask with subtasks and dependencies, and enables AgentCommunication for inter-agent coordination.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Multi-agent coordination algorithms, agent selection, task distribution (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── MultiAgentOrchestration/
      ├── AgentRole.cs                  # Enum (Executor, Linter, Security, Reviewer, Tester, Architect, Debugger)
      ├── AgentStatus.cs                # Enum (Idle, Thinking, Working, Waiting, Blocked, Completed, Failed)
      ├── AgentInstance.cs              # Entity representing an agent instance
      ├── OrchestrationTask.cs          # Entity for orchestration tasks
      ├── AgentCommunication.cs        # Entity for inter-agent communication
      ├── AgentOrchestration.cs         # AggregateRoot for multi-agent orchestration
      └── Events/
          ├── AgentOrchestrationStartedEvent.cs
          ├── AgentAssignedEvent.cs
          └── AgentCommunicationEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── MultiAgentOrchestrationAlgorithms.fs
      ├── SelectAgentForTask          # Select appropriate agent for task
      ├── DistributeSubtasks           # Distribute subtasks among agents
      ├── CoordinateAgents             # Coordinate multiple agents
      ├── HandleAgentCommunication    # Handle inter-agent communication
      ├── UpdateAgentPerformance      # Update agent performance scores
      └── ResolveAgentConflicts        # Resolve conflicts between agents

Libr4.IDE.Application/
  └── MultiAgentOrchestration/
      ├── Commands/
          ├── StartAgentOrchestrationCommand.cs
          ├── AssignAgentCommand.cs
      ├── Queries/
          ├── GetAgentOrchestrationQuery.cs
      ├── DTOs/
          ├── AgentInstanceDto.cs
          ├── OrchestrationTaskDto.cs
          ├── AgentOrchestrationDto.cs
      ├── Handlers/
          ├── StartAgentOrchestrationCommandHandler.cs
          ├── AssignAgentCommandHandler.cs
      └── Validators/
          └── StartAgentOrchestrationCommandValidator.cs

Libr4.IDE.Api/
  └── MultiAgentOrchestrationEndpoints.cs     # Minimal API endpoints
```

## Domain Model

### AgentRole Enum

```csharp
public enum AgentRole
{
    Executor,      // Executes code changes
    Linter,        // Code linting and style checks
    Security,      // Security reviews
    Reviewer,      // Code reviews
    Tester,        // Testing
    Architect,     // Architecture reviews
    Debugger       // Debugging
}
```

### AgentStatus Enum

```csharp
public enum AgentStatus
{
    Idle,          // Agent not working
    Thinking,      // Agent is processing
    Working,       // Agent is executing
    Waiting,       // Agent waiting for input
    Blocked,       // Agent blocked by dependency
    Completed,     // Agent completed task
    Failed         // Agent failed task
}
```

### AgentInstance Entity

```csharp
public class AgentInstance
{
    public Guid Id { get; }
    public AgentRole Role { get; }
    public string AgentName { get; }
    public AgentStatus Status { get; }
    public double PerformanceScore { get; }
    public List<string> Capabilities { get; }
    public Dictionary<string, object> SpecializationProfile { get; }
}
```

### OrchestrationTask Entity

```csharp
public class OrchestrationTask
{
    public Guid Id { get; }
    public string TaskId { get; }
    public string Description { get; }
    public List<OrchestrationTask> Subtasks { get; }
    public List<string> Dependencies { get; }
    public Guid? AssignedAgentId { get; }
    public string Status { get; }
}
```

### AgentCommunication Entity

```csharp
public class AgentCommunication
{
    public Guid Id { get; }
    public Guid FromAgentId { get; }
    public Guid ToAgentId { get; }
    public string Message { get; }
    public string MessageType { get; }
    public DateTime SentAt { get; }
}
```

### AgentOrchestration AggregateRoot

```csharp
public class AgentOrchestration : AggregateRoot<Guid>
{
    public string OrchestrationId { get; }
    public List<AgentInstance> Agents { get; }
    public OrchestrationTask MainTask { get; }
    public List<AgentCommunication> Communications { get; }
    public string Status { get; }
    public DateTime StartedAt { get; }
    public DateTime? CompletedAt { get; }
}
```

## F# Algorithms

### SelectAgentForTask

Selects appropriate agent for a task:

```fsharp
let selectAgentForTask (task: OrchestrationTask) (availableAgents: AgentInstance list) : AgentInstance =
    match task.Description.ToLower() with
    | desc when desc.Contains("security") -> 
        availableAgents |> List.find (fun a -> a.Role = Security)
    | desc when desc.Contains("test") -> 
        availableAgents |> List.find (fun a -> a.Role = Tester)
    | desc when desc.Contains("review") -> 
        availableAgents |> List.find (fun a -> a.Role = Reviewer)
    | desc when desc.Contains("lint") -> 
        availableAgents |> List.find (fun a -> a.Role = Linter)
    | _ -> 
        availableAgents |> List.maxBy (fun a -> a.PerformanceScore)
```

### DistributeSubtasks

Distributes subtasks among agents:

```fsharp
let distributeSubtasks (task: OrchestrationTask) (agents: AgentInstance list) : Map<Guid, OrchestrationTask list> =
    let subtasks = task.Subtasks
    let agentCount = agents.Length
    
    subtasks
    |> List.mapi (fun i subtask ->
        let agentIndex = i % agentCount
        let agent = agents.[agentIndex]
        (agent.Id, subtask)
    )
    |> List.groupBy fst
    |> Map.ofList
    |> Map.map (fun agentId tasks -> tasks |> List.map snd)
```

### CoordinateAgents

Coordinates multiple agents:

```fsharp
let coordinateAgents (orchestration: AgentOrchestration) : unit =
    // Check agent dependencies
    // Handle communication between agents
    // Resolve conflicts
    ()
```

### UpdateAgentPerformance

Updates agent performance scores:

```fsharp
let updateAgentPerformance (agent: AgentInstance) (success: bool) (qualityScore: double) : AgentInstance =
    let newScore = 
        if success then
            agent.PerformanceScore * 0.9 + qualityScore * 0.1
        else
            agent.PerformanceScore * 0.9
    
    { agent with PerformanceScore = newScore }
```

## Application Layer (C#)

### Command Handler

```csharp
public class StartAgentOrchestrationCommandHandler : IRequestHandler<StartAgentOrchestrationCommand, AgentOrchestrationDto>
{
    private readonly IMultiAgentOrchestrationAlgorithms _algorithms;
    
    public async Task<AgentOrchestrationDto> Handle(StartAgentOrchestrationCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var agentOrchestration = _algorithms.StartAgentOrchestration(
            request.TaskId,
            request.MainTask,
            request.AvailableAgents
        );
        
        // Map to DTO
        return agentOrchestration.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class MultiAgentOrchestrationEndpoints
{
    public static void MapMultiAgentOrchestrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/multi-agent")
            .WithTags("Multi-Agent Orchestration")
            .RequireAuthorization();
        
        group.MapPost("/start-orchestration", async (
            StartAgentOrchestrationCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("StartAgentOrchestration")
        .WithOpenApi();
    }
}
```

## Agent Roles and Responsibilities

| Agent Role | Responsibilities | Typical Tasks |
|-------------|----------------|---------------|
| Executor | Execute code changes | Multi-file edits, implementation |
| Linter | Code linting and style | Style checks, linting |
| Security | Security reviews | Vulnerability scanning, compliance |
| Reviewer | Code reviews | Quality reviews, recommendations |
| Tester | Testing | Unit tests, integration tests |
| Architect | Architecture reviews | Architecture validation |
| Debugger | Debugging | Issue resolution, debugging |

## Agent Communication Types

- **Request**: Agent requests information from another agent
- **Response**: Agent responds to a request
- **Notification**: Agent notifies others of status change
- **Coordination**: Agents coordinate on shared task
- **Conflict**: Agents report conflicts

## Testing Strategy

1. **Unit Tests** - Test F# multi-agent coordination algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full multi-agent orchestration flow

## Performance Considerations

- Multi-agent coordination is CPU-light
- Use caching for agent selection
- Implement parallel task execution where possible

## Security Considerations

- Validate all agent communications
- Sanitize messages between agents
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
