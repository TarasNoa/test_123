namespace Libr4.IDE.Domain.FSharp

open System

/// Module for synchronizing F# logic with C# database
/// Provides serialization/deserialization of F# Discriminated Unions
module StatePersistence =
    
    /// Serialize F# AgentState to string for database storage
    let serializeState (state: AgentState) =
        match state with
        | Idle -> "Idle"
        | Processing id -> sprintf "Processing:%O" id
        | Validating result -> sprintf "Validating:%s" result
        | Failed reason -> sprintf "Failed:%s" reason

    /// Deserialize string from database to F# AgentState
    /// Handles empty strings, nulls, and invalid formats gracefully
    let deserializeState (serialized: string) =
        if String.IsNullOrEmpty(serialized) || String.IsNullOrWhiteSpace(serialized) then
            Idle
        else
            let trimmed = serialized.Trim()
            if String.IsNullOrEmpty(trimmed) then
                Idle
            else
                let parts = trimmed.Split(':')
                if parts.Length = 0 || String.IsNullOrEmpty(parts.[0]) then
                    Idle
                else
                    match parts.[0].Trim() with
                    | "Idle" -> Idle
                    | "Processing" -> 
                        if parts.Length > 1 && not (String.IsNullOrWhiteSpace(parts.[1])) then
                            try 
                                Processing(Guid.Parse(parts.[1].Trim()))
                            with _ -> Idle
                        else Idle
                    | "Validating" -> 
                        if parts.Length > 1 then
                            Validating(String.Join(":", parts.[1..]).Trim())
                        else Validating ""
                    | "Failed" -> 
                        if parts.Length > 1 then
                            Failed(String.Join(":", parts.[1..]).Trim())
                        else Failed "Unknown error"
                    | _ -> Idle

    /// Serialize F# AgentEvent to string for database storage
    let serializeEvent (event: AgentEvent) =
        match event with
        | TaskReceived id -> sprintf "TaskReceived:%O" id
        | Success result -> sprintf "Success:%s" result
        | ValidationError msg -> sprintf "ValidationError:%s" msg
        | Timeout -> "Timeout"
        | Cancel -> "Cancel"

    /// Deserialize string from database to F# AgentEvent
    let deserializeEvent (serialized: string) =
        if String.IsNullOrEmpty(serialized) then
            TaskReceived(Guid.Empty)
        else
            let parts = serialized.Split(':')
            if parts.Length = 0 then
                TaskReceived(Guid.Empty)
            else
                match parts.[0] with
                | "TaskReceived" -> 
                    if parts.Length > 1 then
                        try 
                            TaskReceived(Guid.Parse(parts.[1]))
                        with _ -> TaskReceived(Guid.Empty)
                    else TaskReceived(Guid.Empty)
                | "Success" -> 
                    if parts.Length > 1 then
                        Success(String.Join(":", parts.[1..]))
                    else Success ""
                | "ValidationError" -> 
                    if parts.Length > 1 then
                        ValidationError(String.Join(":", parts.[1..]))
                    else ValidationError "Unknown error"
                | "Timeout" -> Timeout
                | "Cancel" -> Cancel
                | _ -> TaskReceived(Guid.Empty)

    /// Create a state snapshot for atomic database operations
    /// This ensures F# state and DB state are always in sync
    let createStateSnapshot (state: AgentState) (timestamp: DateTime) =
        {
            SerializedState = serializeState state
            Timestamp = timestamp
            StateType = match state with
                         | Idle -> "Idle"
                         | Processing _ -> "Processing"
                         | Validating _ -> "Validating"
                         | Failed _ -> "Failed"
        }

    /// Restore state from database snapshot
    let restoreStateFromSnapshot (serialized: string) (timestamp: DateTime) =
        let state = deserializeState serialized
        (state, timestamp)

/// Snapshot record for atomic database operations
type StateSnapshot = {
    SerializedState: string
    Timestamp: DateTime
    StateType: string
}
