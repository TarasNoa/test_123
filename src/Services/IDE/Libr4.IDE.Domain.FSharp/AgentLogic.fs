namespace Libr4.IDE.Domain.FSharp

open AgentTypes

/// Pure state transition logic
/// Invalid transitions are IMPOSSIBLE by design (compiler enforces this)
module AgentLogic =

    /// Main state transition function
    /// Takes current state and event, returns new state
    let nextState (currentState: AgentState) (event: AgentEvent) : AgentState =
        match currentState, event with
        | Idle, TaskReceived id -> Processing id
        | Processing id, Success res -> Validating res
        | Processing _, ValidationError msg -> Failed msg
        | Processing _, Timeout -> Failed "Operation timed out"
        | Processing _, Cancel -> Failed "Operation cancelled"
        | Validating _, ValidationError msg -> Failed msg
        | Validating _, Success _ -> Idle  // Validation passed, back to idle
        | Failed _, TaskReceived id -> Processing id  // Retry from failed state
        | state, event -> 
            // Invalid transition - fail with reason
            Failed $"Invalid transition from {state} with event {event}"

    /// Check if agent is in a terminal state
    let isTerminal (state: AgentState) : bool =
        match state with
        | Failed _ -> true
        | _ -> false

    /// Check if agent is active (can process events)
    let isActive (state: AgentState) : bool =
        not (isTerminal state)

    /// Get current task ID if in Processing state
    let getCurrentTaskId (state: AgentState) : Guid option =
        match state with
        | Processing id -> Some id
        | _ -> None

    /// Get state name for debugging/logging
    let getStateName (state: AgentState) : string =
        match state with
        | Idle -> "Idle"
        | Processing id -> $"Processing({id})"
        | Validating res -> $"Validating({res})"
        | Failed reason -> $"Failed({reason})"

    /// Get event name for debugging/logging
    let getEventName (event: AgentEvent) : string =
        match event with
        | TaskReceived id -> $"TaskReceived({id})"
        | Success res -> $"Success({res})"
        | ValidationError msg -> $"ValidationError({msg})"
        | Timeout -> "Timeout"
        | Cancel -> "Cancel"

// ============================================================================
// C# INTEROP LAYER
// ============================================================================

module AgentCSharpInterop =
    open AgentLogic

    /// Create initial Idle state for C#
    let createIdleState () : obj =
        box Idle

    /// Transition state for C# (takes boxed state and event)
    let transitionState (state: obj) (event: obj) : obj =
        match state, event with
        | :? AgentState as s, :? AgentEvent as e -> 
            let newState = nextState s e
            box newState
        | _ -> 
            box (Failed "Invalid state or event type")

    /// Get state name as string for C#
    let getStateNameForCSharp (state: obj) : string =
        match state with
        | :? AgentState as s -> getStateName s
        | _ -> "Unknown"

    /// Check if state is terminal for C#
    let isTerminalForCSharp (state: obj) : bool =
        match state with
        | :? AgentState as s -> isTerminal s
        | _ -> true

    /// Create TaskReceived event for C#
    let createTaskReceivedEvent (taskId: Guid) : obj =
        box (TaskReceived taskId)

    /// Create Success event for C#
    let createSuccessEvent (result: string) : obj =
        box (Success result)

    /// Create ValidationError event for C#
    let createValidationErrorEvent (message: string) : obj =
        box (ValidationError message)

    /// Create Timeout event for C#
    let createTimeoutEvent () : obj =
        box Timeout

    /// Create Cancel event for C#
    let createCancelEvent () : obj =
        box Cancel
