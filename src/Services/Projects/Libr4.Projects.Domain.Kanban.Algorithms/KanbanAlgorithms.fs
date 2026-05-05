namespace Libr4.Projects.Domain.Kanban.Algorithms

open System
open System.Text.Json
open Libr4.AI.Infrastructure.AI

// Shared types for Kanban algorithms
type CardMovement = {
    CardId: Guid
    FromColumn: string
    ToColumn: string
    Timestamp: DateTime
    Duration: TimeSpan
}

type ColumnMetrics = {
    Name: string
    AverageTime: TimeSpan
    Throughput: float
    BottleneckScore: float
}

// Workflow Optimization Algorithms
module WorkflowOptimizer =

    // Calculate average time cards spend in each column
    let calculateColumnMetrics (movements: CardMovement list) (columnNames: string list) : ColumnMetrics list =
        columnNames
        |> List.map (fun columnName ->
            let columnMovements = 
                movements
                |> List.filter (fun m -> m.ToColumn = columnName)
            
            let averageTime = 
                if columnMovements.IsEmpty then TimeSpan.Zero
                else
                    let total = columnMovements |> List.sumBy (fun m -> m.Duration.TotalMilliseconds)
                    TimeSpan.FromMilliseconds(total / float columnMovements.Length)
            
            let throughput = 
                if averageTime = TimeSpan.Zero then 0.0
                else float columnMovements.Length / averageTime.TotalHours
            
            // Bottleneck score: higher if cards spend more time
            let bottleneckScore = 
                if averageTime = TimeSpan.Zero then 0.0
                else min 1.0 (averageTime.TotalHours / 24.0)
            
            {
                Name = columnName
                AverageTime = averageTime
                Throughput = throughput
                BottleneckScore = bottleneckScore
            })

    // Identify bottlenecks in the workflow
    let identifyBottlenecks (metrics: ColumnMetrics list) : ColumnMetrics list =
        metrics
        |> List.filter (fun m -> m.BottleneckScore > 0.5)
        |> List.sortByDescending (fun m -> m.BottleneckScore)

    // Suggest workflow improvements
    let suggestImprovements (metrics: ColumnMetrics list) : string list =
        let bottlenecks = identifyBottlenecks metrics
        let suggestions = []
        
        if bottlenecks.IsEmpty then
            "No significant bottlenecks detected." :: suggestions
        else
            bottlenecks
            |> List.map (fun b -> 
                sprintf "Column '%s' is a bottleneck (avg time: %.1f hours). Consider: adding more resources, splitting the column, or automating tasks." 
                    b.Name b.AverageTime.TotalHours)
            |> List.append suggestions

    // Identify bottlenecks using AI for intelligent workflow analysis
    let identifyBottlenecksWithAI (aiService: IAIService) (metrics: ColumnMetrics list) (workflowContext: string) : Async<ColumnMetrics list> =
        async {
            let metricsText = metrics |> List.map (fun m -> sprintf "%s: avg %.1f hrs, throughput %.1f, bottleneck %.2f" m.Name m.AverageTime.TotalHours m.Throughput m.BottleneckScore) |> String.concat "; "
            
            let prompt = sprintf "Identify bottlenecks: columns [%s], context '%s'. Return JSON: {\"bottleneckColumnNames\": [string]}" metricsText workflowContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let bottleneckNames = try root.GetProperty("bottleneckColumnNames").EnumerateArray() |> Seq.map (fun n -> n.GetString()) |> Set.ofSeq with _ -> Set.empty
            
            let bottlenecks = 
                metrics
                |> List.filter (fun m -> Set.contains m.Name bottleneckNames)
                |> List.sortByDescending (fun m -> m.BottleneckScore)
            
            return bottlenecks
        }

// Card Movement Analytics
module CardAnalytics =

    type CardFlow = {
        CardId: Guid
        Movements: CardMovement list
        TotalTime: TimeSpan
        CompletionRate: float
        FirstTimestamp: DateTime
        LastTimestamp: DateTime
    }

    // Analyze card flow through the board
    let analyzeCardFlow (movements: CardMovement list) (cardId: Guid) : CardFlow option =
        let cardMovements = movements |> List.filter (fun m -> m.CardId = cardId)
        
        if cardMovements.IsEmpty then None
        else
            let totalTime = 
                cardMovements
                |> List.sumBy (fun m -> m.Duration.TotalMilliseconds)
                |> TimeSpan.FromMilliseconds
            
            // Calculate completion rate based on movements
            let completionRate = 
                let uniqueColumns = cardMovements |> List.map (fun m -> m.ToColumn) |> Set.ofList
                float uniqueColumns.Count / 5.0 // Assuming 5 columns
            
            let firstTimestamp = cardMovements |> List.minBy (fun m -> m.Timestamp) |> fun m -> m.Timestamp
            let lastTimestamp = cardMovements |> List.maxBy (fun m -> m.Timestamp) |> fun m -> m.Timestamp
            
            Some {
                CardId = cardId
                Movements = cardMovements
                TotalTime = totalTime
                CompletionRate = completionRate
                FirstTimestamp = firstTimestamp
                LastTimestamp = lastTimestamp
            }

    // Analyze card flow using AI for intelligent flow analysis
    let analyzeCardFlowWithAI (aiService: IAIService) (movements: CardMovement list) (cardId: Guid) (flowContext: string) : Async<CardFlow option> =
        async {
            let cardMovements = movements |> List.filter (fun m -> m.CardId = cardId)
            
            if cardMovements.IsEmpty then
                return None
            else
                let movementsText = cardMovements |> List.map (fun m -> sprintf "%s -> %s (%.1f hrs)" m.FromColumn m.ToColumn m.Duration.TotalHours) |> String.concat "; "
                
                let prompt = sprintf "Analyze card flow: movements [%s], context '%s'. Return JSON: {\"completionRate\": number (0-1), \"isStuck\": bool}" movementsText flowContext
                
                let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
                
                let jsonDoc = JsonDocument.Parse(aiResponse)
                let root = jsonDoc.RootElement
                
                let totalTime = 
                    cardMovements
                    |> List.sumBy (fun m -> m.Duration.TotalMilliseconds)
                    |> TimeSpan.FromMilliseconds
                
                let uniqueColumns = cardMovements |> List.map (fun m -> m.ToColumn) |> Set.ofList
                let calcCompletionRate() = float uniqueColumns.Count / 5.0
                let completionRate = try root.GetProperty("completionRate").GetDouble() with _ -> calcCompletionRate()
                
                let firstTimestamp = cardMovements |> List.minBy (fun m -> m.Timestamp) |> fun m -> m.Timestamp
                let lastTimestamp = cardMovements |> List.maxBy (fun m -> m.Timestamp) |> fun m -> m.Timestamp
                
                return Some {
                    CardId = cardId
                    Movements = cardMovements
                    TotalTime = totalTime
                    CompletionRate = completionRate
                    FirstTimestamp = firstTimestamp
                    LastTimestamp = lastTimestamp
                }
        }

    // Calculate cycle time (time from start to completion)
    let calculateCycleTime (movements: CardMovement list) (cardId: Guid) : TimeSpan option =
        let cardMovements = movements |> List.filter (fun m -> m.CardId = cardId)
        
        if cardMovements.IsEmpty then None
        else
            let first = cardMovements |> List.minBy (fun m -> m.Timestamp)
            let last = cardMovements |> List.maxBy (fun m -> m.Timestamp)
            Some (last.Timestamp - first.Timestamp)

// WIP Limit Algorithms
module WipManager =

    type WipLimit = {
        ColumnId: Guid
        ColumnName: string
        Limit: int
        CurrentCount: int
        IsExceeded: bool
    }

    // Calculate WIP limits for each column
    let calculateWipLimits (columnIds: Guid list) (columnNames: string list) (cardCounts: int list) (limits: int list) : WipLimit list =
        List.mapi (fun i id ->
            let name = if i < columnNames.Length then columnNames.[i] else ""
            let count = if i < cardCounts.Length then cardCounts.[i] else 0
            let limit = if i < limits.Length then limits.[i] else 10
            {
                ColumnId = id
                ColumnName = name
                Limit = limit
                CurrentCount = count
                IsExceeded = count > limit
            })
            columnIds

    // Suggest optimal WIP limits based on historical data
    let suggestWipLimits (throughputData: float list) (currentLimits: int list) : int list =
        // Simplified: suggest limits based on average throughput
        let avgThroughput = if throughputData.IsEmpty then 1.0 else List.average throughputData
        currentLimits |> List.map (fun limit -> max 1 (int (float limit * avgThroughput)))

    // Suggest optimal WIP limits using AI for intelligent capacity management
    let suggestWipLimitsWithAI (aiService: IAIService) (throughputData: float list) (currentLimits: int list) (wipContext: string) : Async<int list> =
        async {
            let throughputText = throughputData |> List.map (fun t -> sprintf "%.1f" t) |> String.concat "; "
            let limitsText = currentLimits |> List.map (fun l -> string l) |> String.concat "; "
            
            let prompt = sprintf "Suggest WIP limits: throughput [%s], current limits [%s], context '%s'. Return JSON: {\"suggestedLimits\": [number]}" throughputText limitsText wipContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let avgThroughput = if throughputData.IsEmpty then 1.0 else List.average throughputData
            let calcSuggestedLimits() = currentLimits |> List.map (fun limit -> max 1 (int (float limit * avgThroughput)))
            let suggestedLimits = try root.GetProperty("suggestedLimits").EnumerateArray() |> Seq.map (fun l -> l.GetInt32()) |> List.ofSeq with _ -> calcSuggestedLimits()
            
            return suggestedLimits
        }

// Burndown Algorithms
module BurndownAnalyzer =

    type BurndownPoint = {
        Date: DateTime
        RemainingWork: int
        IdealRemaining: int
    }

    // Calculate burndown chart data
    let calculateBurndown (startDate: DateTime) (endDate: DateTime) (initialWork: int) (completedWork: int list) : BurndownPoint list =
        let totalDays = int (endDate - startDate).TotalDays
        let idealPerDay = float initialWork / float totalDays
        
        [0 .. totalDays]
        |> List.mapi (fun i day ->
            let date = startDate.AddDays(float day)
            let completedSoFar = 
                if i < completedWork.Length then completedWork.[i]
                else completedWork |> List.sum
            
            let remaining = max 0 (initialWork - completedSoFar)
            let idealRemaining = max 0 (initialWork - int (float day * idealPerDay))
            
            {
                Date = date
                RemainingWork = remaining
                IdealRemaining = idealRemaining
            })

    // Predict completion date based on current velocity
    let predictCompletion (remainingWork: int) (velocity: float) (startDate: DateTime) : DateTime =
        if velocity <= 0.0 then DateTime.MaxValue
        else
            let daysNeeded = float remainingWork / velocity
            startDate.AddDays(daysNeeded)

    // Predict completion date using AI for intelligent forecasting
    let predictCompletionWithAI (aiService: IAIService) (remainingWork: int) (velocity: float) (startDate: DateTime) (burndownContext: string) : Async<DateTime> =
        async {
            let prompt = sprintf "Predict completion: remaining %d, velocity %.1f, start %s, context '%s'. Return JSON: {\"daysNeeded\": number}" remainingWork velocity (startDate.ToString("o")) burndownContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcDaysNeeded() = if velocity <= 0.0 then 999999.9 else float remainingWork / velocity
            let daysNeeded = try root.GetProperty("daysNeeded").GetDouble() with _ -> calcDaysNeeded()
            
            if daysNeeded > 999999.0 then
                return DateTime.MaxValue
            else
                return startDate.AddDays(daysNeeded)
        }

// Priority Algorithms
module PriorityManager =

    type PriorityScore = {
        CardId: Guid
        Score: float
        Factors: Map<string, float>
    }

    // Calculate priority score based on multiple factors
    let calculatePriorityScore (dueDate: DateTime option) (priority: int) (dependenciesCount: int) (stakeholderImportance: float) : float =
        let dueDateScore = 
            match dueDate with
            | None -> 0.0
            | Some date ->
                let daysUntilDue = (date - DateTime.UtcNow).TotalDays
                if daysUntilDue < 0.0 then 1.0 // Overdue
                elif daysUntilDue < 7.0 then 0.8 // Due within a week
                elif daysUntilDue < 14.0 then 0.5 // Due within two weeks
                else 0.1 // Not urgent
        
        let priorityScore = float priority / 10.0 // Normalize 1-10 to 0.0-1.0
        let dependencyScore = 1.0 - min 1.0 (float dependenciesCount / 10.0) // Fewer dependencies = higher score
        
        // Weighted combination
        dueDateScore * 0.4 + priorityScore * 0.3 + dependencyScore * 0.2 + stakeholderImportance * 0.1

    // Calculate priority score using AI for intelligent prioritization
    let calculatePriorityScoreWithAI (aiService: IAIService) (dueDate: DateTime option) (priority: int) (dependenciesCount: int) (stakeholderImportance: float) (priorityContext: string) : Async<float> =
        async {
            let dueDateText = match dueDate with | None -> "none" | Some d -> d.ToString("o")
            
            let prompt = sprintf "Calculate priority: due %s, priority %d, deps %d, importance %.1f, context '%s'. Return JSON: {\"score\": number (0-1)}" dueDateText priority dependenciesCount stakeholderImportance priorityContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let dueDateScore = 
                match dueDate with
                | None -> 0.0
                | Some date ->
                    let daysUntilDue = (date - DateTime.UtcNow).TotalDays
                    if daysUntilDue < 0.0 then 1.0
                    elif daysUntilDue < 7.0 then 0.8
                    elif daysUntilDue < 14.0 then 0.5
                    else 0.1
            
            let priorityScore = float priority / 10.0
            let dependencyScore = 1.0 - min 1.0 (float dependenciesCount / 10.0)
            let calcScore() = dueDateScore * 0.4 + priorityScore * 0.3 + dependencyScore * 0.2 + stakeholderImportance * 0.1
            let score = try root.GetProperty("score").GetDouble() with _ -> calcScore()
            
            return score
        }

    // Prioritize cards based on calculated scores
    let prioritizeCards (cardIds: Guid list) (dueDates: DateTime option list) (priorities: int list) (dependenciesCounts: int list) (stakeholderImportances: float list) : (Guid * float) list =
        List.mapi (fun i id ->
            let dueDate = if i < dueDates.Length then dueDates.[i] else None
            let priority = if i < priorities.Length then priorities.[i] else 5
            let deps = if i < dependenciesCounts.Length then dependenciesCounts.[i] else 0
            let importance = if i < stakeholderImportances.Length then stakeholderImportances.[i] else 0.5
            let score = calculatePriorityScore dueDate priority deps importance
            (id, score))
            cardIds
        |> List.sortByDescending snd
