namespace Libr4.IDE.Domain.FSharp

open System

/// Simplified agent state machine types
/// Discriminated Unions ensure agents CANNOT enter invalid states
type AgentState = 
    | Idle 
    | Processing of taskId: Guid 
    | Validating of result: string 
    | Failed of reason: string

/// Agent events that trigger state transitions
type AgentEvent =
    | TaskReceived of taskId: Guid
    | Success of result: string
    | ValidationError of message: string
    | Timeout
    | Cancel

/// Priority for agent tasks
type TaskPriority = Critical | High | Normal | Low

/// Simple agent task definition
type SimpleAgentTask = {
    TaskId: Guid
    TaskType: string
    Description: string
    Priority: TaskPriority
}
