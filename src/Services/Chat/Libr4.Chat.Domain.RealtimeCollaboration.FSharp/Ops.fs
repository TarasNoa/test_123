namespace Libr4.Chat.Domain.RealtimeCollaboration.FSharp

open System

module CRDTOps =
    let createOperation (docId: Guid) (userId: Guid) (opType: OperationType) (pos: int) (content: string option) (now: DateTimeOffset) (version: int) : Operation =
        {
            id = Guid.NewGuid()
            documentId = docId
            userId = userId
            opType = opType
            position = pos
            content = content
            timestamp = now
            version = version
        }

    let applyOperation (state: CRDTState) (op: Operation) : CRDTState =
        match op.opType with
        | OperationType.Insert ->
            let newContent = 
                if op.position <= state.content.Length then
                    state.content.Insert(op.position, op.content |> Option.defaultValue "")
                else state.content
            { state with content = newContent; version = op.version; lastModified = op.timestamp; operations = state.operations @ [op] }
        | OperationType.Delete ->
            let length = op.content |> Option.map String.length |> Option.defaultValue 1
            let newContent = 
                if op.position < state.content.Length then
                    state.content.Remove(op.position, min length (state.content.Length - op.position))
                else state.content
            { state with content = newContent; version = op.version; lastModified = op.timestamp; operations = state.operations @ [op] }
        | OperationType.Update ->
            let newContent = 
                if op.position < state.content.Length then
                    let before = state.content.Substring(0, op.position)
                    let after = state.content.Substring(op.position + 1)
                    before + (op.content |> Option.defaultValue "") + after
                else state.content
            { state with content = newContent; version = op.version; lastModified = op.timestamp; operations = state.operations @ [op] }

    let resolveConflict (op1: Operation) (op2: Operation) (resolution: ConflictResolution) : Operation =
        match resolution with
        | ConflictResolution.ClientWins -> op1
        | ConflictResolution.ServerWins -> op2
        | ConflictResolution.Merge -> if op1.timestamp < op2.timestamp then op1 else op2

module CollaborationOps =
    let initializeState (docId: Guid) (content: string) (now: DateTimeOffset) : CRDTState =
        {
            documentId = docId
            content = content
            version = 1
            lastModified = now
            operations = []
        }

    let getOperationsSince (state: CRDTState) (version: int) : Operation list =
        state.operations |> List.filter (fun op -> op.version > version)
