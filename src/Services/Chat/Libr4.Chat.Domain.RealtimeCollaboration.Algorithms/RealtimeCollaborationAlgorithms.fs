namespace Libr4.Chat.Domain.RealtimeCollaboration.Algorithms

open System
open System.Text.Json
open Libr4.Chat.Domain.RealtimeCollaboration
open Libr4.AI.Infrastructure.AI

// CRDT Operations
module CRDTOperations =

    type OperationResult = {
        NewContent: string
        NewVersion: int
        ConflictDetected: bool
    }

    // Apply CRDT operation to document content
    let applyOperation (content: string) (version: int) (opType: OperationType) (opContent: string option) (position: int) : OperationResult =
        let newContent = 
            match opType, opContent with
            | OperationType.Insert, Some c -> 
                if position >= content.Length then content + c
                else content.[0..position] + c + content.[position..]
            | OperationType.Delete, Some c -> content.Replace(c, "")
            | OperationType.Update, Some c -> c
            | _ -> content
        
        {
            NewContent = newContent
            NewVersion = version + 1
            ConflictDetected = false
        }

// Conflict Resolver
module ConflictResolver =

    type ConflictResolutionResult = {
        Resolution: ConflictResolution
        ResolvedContent: string
        Reason: string
    }

    // Resolve conflicts between concurrent operations
    let resolveConflict (content: string) (op1: OperationType option) (op2: OperationType option) (op1Content: string option) (op2Content: string option) : ConflictResolutionResult =
        match op1, op2 with
        | Some OperationType.Insert, Some OperationType.Delete ->
            {
                Resolution = ConflictResolution.Merge
                ResolvedContent = content
                Reason = "Insert and Delete conflict - merged by keeping original"
            }
        | Some OperationType.Delete, Some OperationType.Insert ->
            {
                Resolution = ConflictResolution.Merge
                ResolvedContent = content
                Reason = "Delete and Insert conflict - merged by keeping original"
            }
        | Some OperationType.Update, Some OperationType.Update ->
            {
                Resolution = ConflictResolution.ServerWins
                ResolvedContent = op2Content |> Option.defaultValue content
                Reason = "Both operations are Update - server wins"
            }
        | _ ->
            {
                Resolution = ConflictResolution.ClientWins
                ResolvedContent = op1Content |> Option.defaultValue content
                Reason = "No direct conflict - client wins"
            }

    // Resolve conflict using AI for intelligent conflict resolution
    let resolveConflictWithAI (aiService: IAIService) (content: string) (op1: OperationType option) (op2: OperationType option) (op1Content: string option) (op2Content: string option) (collaborationContext: string) : Async<ConflictResolutionResult> =
        async {
            let op1Text = match op1 with | Some op -> string op | None -> "None"
            let op2Text = match op2 with | Some op -> string op | None -> "None"
            let op1ContentText = op1Content |> Option.defaultValue "None"
            let op2ContentText = op2Content |> Option.defaultValue "None"
            
            let prompt = sprintf "Resolve conflict: content '%s', op1 %s (%s), op2 %s (%s), context '%s'. Return JSON: {\"resolution\": \"ClientWins/ServerWins/Merge\", \"reason\": string}" content op1Text op1ContentText op2Text op2ContentText collaborationContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let resolutionStr = try root.GetProperty("resolution").GetString() with _ -> "ClientWins"
            let resolution = 
                match resolutionStr with
                | "ServerWins" -> ConflictResolution.ServerWins
                | "Merge" -> ConflictResolution.Merge
                | _ -> ConflictResolution.ClientWins
            
            let reason = try root.GetProperty("reason").GetString() with _ -> "AI-based resolution"
            
            let resolvedContent = 
                match resolution with
                | ConflictResolution.ServerWins -> op2Content |> Option.defaultValue content
                | ConflictResolution.Merge -> content
                | ConflictResolution.ClientWins -> op1Content |> Option.defaultValue content
            
            return {
                Resolution = resolution
                ResolvedContent = resolvedContent
                Reason = reason
            }
        }

// Synchronization Engine
module SynchronizationEngine =

    type SyncState = {
        DocumentId: Guid
        Version: int
        PendingOperations: int
        LastSyncTime: DateTimeOffset
        IsSyncing: bool
    }

    // Track document synchronization state
    let trackSync (documentId: Guid) (version: int) (pendingOps: int) (now: DateTimeOffset) : SyncState =
        {
            DocumentId = documentId
            Version = version
            PendingOperations = pendingOps
            LastSyncTime = now
            IsSyncing = pendingOps > 0
        }

    // Check if synchronization is needed
    let needsSync (syncState: SyncState) (serverVersion: int) : bool =
        syncState.Version < serverVersion || syncState.PendingOperations > 0

    // Track synchronization using AI for intelligent sync prediction
    let trackSyncWithAI (aiService: IAIService) (documentId: Guid) (version: int) (pendingOps: int) (now: DateTimeOffset) (syncContext: string) : Async<SyncState> =
        async {
            let prompt = sprintf "Predict sync state: version %d, pending %d ops, context '%s'. Return JSON: {\"isSyncing\": bool, \"syncPriority\": number (0-1)}" version pendingOps syncContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let isSyncingAI = try root.GetProperty("isSyncing").GetBoolean() with _ -> pendingOps > 0
            
            return {
                DocumentId = documentId
                Version = version
                PendingOperations = pendingOps
                LastSyncTime = now
                IsSyncing = isSyncingAI
            }
        }
