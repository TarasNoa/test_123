namespace Libr4.Chat.Domain.ChatsCollaboration.Algorithms

open System
open System.Text.Json
open Libr4.Chat.Domain.ChatsCollaboration
open Libr4.AI.Application.Abstractions

// Conflict Resolution Engine
module ConflictResolutionEngine =

    type ConflictResolution = {
        ResolutionStrategy: string
        WinnerUserId: Guid option
        MergedContent: string option
        RequiresManualReview: bool
    }

    // Resolve conflict between two operations
    let resolveConflict (operation1: CollaborationOperation) (operation2: CollaborationOperation) : ConflictResolution =
        // Simple last-write-wins strategy (to be enhanced with ML per memory rules)
        if operation1.Timestamp > operation2.Timestamp then
            {
                ResolutionStrategy = "last-write-wins"
                WinnerUserId = Some operation1.UserId
                MergedContent = None
                RequiresManualReview = false
            }
        else
            {
                ResolutionStrategy = "last-write-wins"
                WinnerUserId = Some operation2.UserId
                MergedContent = None
                RequiresManualReview = false
            }

    // Resolve conflict using AI for intelligent merging
    let resolveConflictWithAI (aiService: IAIService) (operation1: CollaborationOperation) (operation2: CollaborationOperation) (context: string) : Async<ConflictResolution> =
        async {
            let op1Text = sprintf "User %s at %s: %s" (operation1.UserId.ToString()) (operation1.Timestamp.ToString("o")) operation1.OperationType
            let op2Text = sprintf "User %s at %s: %s" (operation2.UserId.ToString()) (operation2.Timestamp.ToString("o")) operation2.OperationType
            
            let prompt = sprintf "Resolve conflict between operations: Op1 [%s], Op2 [%s], context '%s'. Return JSON: {\"resolutionStrategy\": string, \"winnerUserId\": string (guid or empty), \"mergedContent\": string or empty, \"requiresManualReview\": bool}" op1Text op2Text context
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let strategy = try root.GetProperty("resolutionStrategy").GetString() with _ -> "last-write-wins"
            let winnerIdStr = try root.GetProperty("winnerUserId").GetString() with _ -> ""
            let winnerUserId = if String.IsNullOrEmpty(winnerIdStr) then None else Some (Guid.Parse(winnerIdStr))
            let mergedContentStr = try root.GetProperty("mergedContent").GetString() with _ -> ""
            let mergedContent = if String.IsNullOrEmpty(mergedContentStr) then None else Some mergedContentStr
            let requiresManualReview = try root.GetProperty("requiresManualReview").GetBoolean() with _ -> false
            
            return {
                ResolutionStrategy = strategy
                WinnerUserId = winnerUserId
                MergedContent = mergedContent
                RequiresManualReview = requiresManualReview
            }
        }

// Session Analytics
module SessionAnalytics =

    type SessionMetrics = {
        TotalDuration: int
        AverageOperationsPerMinute: float
        ConflictRate: float
        ParticipantEngagement: Map<Guid, int>
        MostActiveUser: Guid option
    }

    // Analyze collaboration session metrics
    let analyzeSession (session: CollaborationSession) (operations: CollaborationOperation list) : SessionMetrics =
        let duration = session.DurationSeconds |> Option.ofNullable |> Option.defaultValue 0
        let opCount = session.OperationsCount
        let conflictCount = session.ConflictsCount
        
        let avgOpsPerMinute = 
            if duration > 0 then float opCount / (float duration / 60.0)
            else 0.0
        
        let conflictRate = 
            if opCount > 0 then float conflictCount / float opCount
            else 0.0
        
        let engagement = 
            operations
            |> List.groupBy (fun op -> op.UserId)
            |> List.map (fun (userId, ops) -> (userId, ops.Length))
            |> Map.ofList
        
        let mostActiveUser =
            if engagement.IsEmpty then None
            else engagement |> Map.toList |> List.maxBy (fun (_, count) -> count) |> fst |> Some
        
        {
            TotalDuration = duration
            AverageOperationsPerMinute = avgOpsPerMinute
            ConflictRate = conflictRate
            ParticipantEngagement = engagement
            MostActiveUser = mostActiveUser
        }

    // Analyze session using AI for deeper insights
    let analyzeSessionWithAI (aiService: IAIService) (session: CollaborationSession) (operations: CollaborationOperation list) : Async<SessionMetrics> =
        async {
            let duration = session.DurationSeconds |> Option.ofNullable |> Option.defaultValue 0
            let opCount = session.OperationsCount
            let conflictCount = session.ConflictsCount
            
            let operationsText = operations |> List.map (fun op -> sprintf "User %s: %s at %s" (op.UserId.ToString()) op.OperationType (op.Timestamp.ToString("o"))) |> String.concat " | "
            
            let prompt = sprintf "Analyze collaboration session: %d operations, %d conflicts, duration %d sec. Operations: [%s]. Return JSON: {\"averageOpsPerMinute\": number, \"conflictRate\": number (0-1), \"requiresManualReview\": bool}" opCount conflictCount duration operationsText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let avgOpsPerMinute = try root.GetProperty("averageOpsPerMinute").GetDouble() with _ -> float opCount / (if duration > 0 then float duration / 60.0 else 1.0)
            let conflictRate = try root.GetProperty("conflictRate").GetDouble() with _ -> if opCount > 0 then float conflictCount / float opCount else 0.0
            
            let engagement = 
                operations
                |> List.groupBy (fun op -> op.UserId)
                |> List.map (fun (userId, ops) -> (userId, ops.Length))
                |> Map.ofList
            
            let mostActiveUser =
                if engagement.IsEmpty then None
                else engagement |> Map.toList |> List.maxBy (fun (_, count) -> count) |> fst |> Some
            
            return {
                TotalDuration = duration
                AverageOperationsPerMinute = avgOpsPerMinute
                ConflictRate = conflictRate
                ParticipantEngagement = engagement
                MostActiveUser = mostActiveUser
            }
        }

// Real-time Sync Engine
module RealtimeSyncEngine =

    type SyncOperation = {
        OperationId: Guid
        Type: string
        Data: Map<string, obj>
        Version: int
        Timestamp: DateTimeOffset
    }

    type SyncState = {
        CurrentVersion: int
        PendingOperations: SyncOperation list
        ConflictedOperations: SyncOperation list
    }

    // Apply operation to sync state
    let applyOperation (state: SyncState) (operation: SyncOperation) : SyncState =
        if operation.Version = state.CurrentVersion + 1 then
            {
                CurrentVersion = operation.Version
                PendingOperations = state.PendingOperations |> List.filter (fun op -> op.OperationId <> operation.OperationId)
                ConflictedOperations = state.ConflictedOperations
            }
        else
            {
                CurrentVersion = state.CurrentVersion
                PendingOperations = operation :: state.PendingOperations
                ConflictedOperations = operation :: state.ConflictedOperations
            }

// Comment Thread Analyzer
module CommentThreadAnalyzer =

    type ThreadSummary = {
        CommentCount: int
        ResolvedCount: int
        AverageResolutionTime: TimeSpan option
        MostActiveCommenter: Guid option
        HotTopics: string list
    }

    // Analyze comment thread for insights
    let analyzeThread (comments: InlineComment list) : ThreadSummary =
        let commentCount = comments.Length
        let resolvedComments = comments |> List.filter (fun c -> c.IsResolved)
        let resolvedCount = resolvedComments.Length
        
        let avgResolutionTime =
            if resolvedCount = 0 then None
            else
                let resolutions = 
                    resolvedComments
                    |> List.choose (fun c -> 
                        c.ResolvedAt |> Option.ofNullable |> Option.map (fun ra -> ra - c.CreatedAt)
                    )
                if resolutions.IsEmpty then None
                else 
                    let avgTicks = resolutions |> List.map (fun ts -> float ts.Ticks) |> List.average |> int64
                    Some (TimeSpan.FromTicks(avgTicks))
        
        let commenterCounts = 
            comments
            |> List.groupBy (fun c -> c.UserId)
            |> List.map (fun (userId, comments) -> (userId, comments.Length))
        
        let mostActiveCommenter =
            if commenterCounts.IsEmpty then None
            else commenterCounts |> List.maxBy (fun (_, count) -> count) |> fst |> Some
        
        let hotTopics =
            comments
            |> List.map (fun c -> c.Comment.ToLower())
            |> List.collect (fun comment -> comment.Split(' ') |> Array.toList)
            |> List.filter (fun word -> word.Length > 4)
            |> List.countBy id
            |> List.sortByDescending snd
            |> List.truncate 5
            |> List.map fst
        
        {
            CommentCount = commentCount
            ResolvedCount = resolvedCount
            AverageResolutionTime = avgResolutionTime
            MostActiveCommenter = mostActiveCommenter
            HotTopics = hotTopics
        }

    // Analyze comment thread using AI for sentiment and topic extraction
    let analyzeThreadWithAI (aiService: IAIService) (comments: InlineComment list) : Async<ThreadSummary> =
        async {
            let commentCount = comments.Length
            let resolvedComments = comments |> List.filter (fun c -> c.IsResolved)
            let resolvedCount = resolvedComments.Length
            
            let commentsText = comments |> List.map (fun c -> sprintf "User %s: %s (resolved: %b)" (c.UserId.ToString()) c.Comment c.IsResolved) |> String.concat " | "
            
            let prompt = sprintf "Analyze comment thread: %d comments, %d resolved. Comments: [%s]. Return JSON: {\"averageResolutionHours\": number, \"mostActiveCommenter\": string (guid or empty), \"hotTopics\": [string]}" commentCount resolvedCount commentsText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let avgResolutionHours = try root.GetProperty("averageResolutionHours").GetDouble() with _ -> 24.0
            let avgResolutionTime = Some (TimeSpan.FromHours(avgResolutionHours))
            
            let mostActiveIdStr = try root.GetProperty("mostActiveCommenter").GetString() with _ -> ""
            let mostActiveCommenter = if String.IsNullOrEmpty(mostActiveIdStr) then None else Some (Guid.Parse(mostActiveIdStr))
            
            let hotTopics = 
                try
                    root.GetProperty("hotTopics").EnumerateArray()
                    |> Seq.map (fun t -> t.GetString())
                    |> List.ofSeq
                with _ ->
                    comments
                    |> List.map (fun c -> c.Comment.ToLower())
                    |> List.collect (fun comment -> comment.Split(' ') |> Array.toList)
                    |> List.filter (fun word -> word.Length > 4)
                    |> List.countBy id
                    |> List.sortByDescending snd
                    |> List.truncate 5
                    |> List.map fst
            
            return {
                CommentCount = commentCount
                ResolvedCount = resolvedCount
                AverageResolutionTime = avgResolutionTime
                MostActiveCommenter = mostActiveCommenter
                HotTopics = hotTopics
            }
        }

// QA Prioritizer
module QAPrioritizer =

    type PriorityScore = {
        Score: float
        Factors: string list
        RecommendedPriority: QAPriority
    }

    // Calculate priority score for QA question
    let calculatePriority (qa: AnonymousQA) (totalQuestions: int) : PriorityScore =
        let factors = ResizeArray<string>()
        let mutable score = 50.0
        
        // Upvotes increase priority
        score <- score + (float qa.Upvotes * 2.0)
        if qa.Upvotes > 5 then factors.Add("High upvote count")
        
        // Age of question
        let daysOld = qa.DaysSinceCreated
        if daysOld > 7 then
            score <- score + (float daysOld * 0.5)
            factors.Add(sprintf "Question is %d days old" daysOld)
        
        // Category weighting
        match qa.Category with
        | QACategory.Technical -> 
            score <- score + 10.0
            factors.Add("Technical question")
        | QACategory.Budget ->
            score <- score + 15.0
            factors.Add("Budget-related question")
        | QACategory.Timeline ->
            score <- score + 12.0
            factors.Add("Timeline question")
        | _ -> ()
        
        let recommendedPriority = 
            match score with
            | _ when score >= 80.0 -> QAPriority.Urgent
            | _ when score >= 60.0 -> QAPriority.High
            | _ when score >= 40.0 -> QAPriority.Normal
            | _ -> QAPriority.Low
        
        {
            Score = score
            Factors = List.ofSeq factors
            RecommendedPriority = recommendedPriority
        }

    // Calculate priority using AI for intelligent scoring
    let calculatePriorityWithAI (aiService: IAIService) (qa: AnonymousQA) (totalQuestions: int) (categoryContext: string) : Async<PriorityScore> =
        async {
            let prompt = sprintf "Calculate priority for QA: upvotes %d, days old %d, category '%s', total questions %d, context '%s'. Return JSON: {\"score\": number (0-100), \"factors\": [string], \"recommendedPriority\": \"Urgent/High/Normal/Low\"}" qa.Upvotes qa.DaysSinceCreated (string qa.Category) totalQuestions categoryContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let score = try root.GetProperty("score").GetDouble() with _ -> 50.0
            
            let factors = 
                try
                    root.GetProperty("factors").EnumerateArray()
                    |> Seq.map (fun f -> f.GetString())
                    |> List.ofSeq
                with _ ->
                    let fallbackFactors = ResizeArray<string>()
                    if qa.Upvotes > 5 then fallbackFactors.Add("High upvote count")
                    if qa.DaysSinceCreated > 7 then fallbackFactors.Add(sprintf "Question is %d days old" qa.DaysSinceCreated)
                    List.ofSeq fallbackFactors
            
            let priorityStr = try root.GetProperty("recommendedPriority").GetString() with _ -> "Normal"
            let recommendedPriority = 
                match priorityStr with
                | "Urgent" -> QAPriority.Urgent
                | "High" -> QAPriority.High
                | "Low" -> QAPriority.Low
                | _ -> QAPriority.Normal
            
            return {
                Score = score
                Factors = factors
                RecommendedPriority = recommendedPriority
            }
        }
