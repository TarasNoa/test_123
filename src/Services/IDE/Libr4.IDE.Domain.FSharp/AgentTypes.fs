namespace Libr4.IDE.Domain.FSharp

open System

/// Agent events that trigger state transitions
type AgentEvent =
    | TaskReceived of taskId: Guid
    | Success of result: string
    | ValidationError of message: string
    | Timeout
    | Cancel
