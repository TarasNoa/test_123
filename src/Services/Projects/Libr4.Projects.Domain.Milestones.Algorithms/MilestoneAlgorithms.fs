namespace Libr4.Projects.Domain.Milestones.Algorithms

open System
open System.Text.Json
open Libr4.AI.Infrastructure.AI

// Milestone Progress Tracking
module MilestoneProgressTracker =

    type MilestoneProgress = {
        MilestoneId: Guid
        Name: string
        TargetDate: DateTime
        CurrentProgress: int
        EstimatedCompletion: DateTime option
        OnTrack: bool
    }

    type TaskProgress = {
        TaskId: Guid
        Completed: bool
        Progress: int
    }

    // Calculate milestone progress based on task completion
    let calculateProgress (tasks: TaskProgress list) : int =
        if tasks.IsEmpty then 0
        else
            let totalProgress = tasks |> List.sumBy (fun t -> t.Progress)
            totalProgress / List.length tasks

    // Estimate milestone completion date based on current progress
    let estimateCompletionDate (startDate: DateTime) (targetDate: DateTime) (currentProgress: int) : DateTime option =
        if currentProgress = 0 then None
        else
            let totalDuration = (targetDate - startDate).Days
            let elapsedDuration = float totalDuration * (float currentProgress / 100.0)
            let estimatedDays = float totalDuration / (float currentProgress / 100.0)
            Some (startDate.AddDays(estimatedDays))

    // Determine if milestone is on track
    let isOnTrack (currentProgress: int) (targetDate: DateTime) (startDate: DateTime) : bool =
        let now = DateTime.UtcNow
        let totalDuration = (targetDate - startDate).Days
        let elapsedDays = (now - startDate).Days
        
        if totalDuration <= 0 then true
        else
            let expectedProgress = int ((float elapsedDays / float totalDuration) * 100.0)
            currentProgress >= expectedProgress - 10  // Allow 10% variance

    // Track milestone progress
    let trackMilestoneProgress (milestoneId: Guid) (name: string) (targetDate: DateTime) (startDate: DateTime) (tasks: TaskProgress list) : MilestoneProgress =
        let progress = calculateProgress tasks
        let estimatedCompletion = estimateCompletionDate startDate targetDate progress
        let onTrack = isOnTrack progress targetDate startDate
        
        {
            MilestoneId = milestoneId
            Name = name
            TargetDate = targetDate
            CurrentProgress = progress
            EstimatedCompletion = estimatedCompletion
            OnTrack = onTrack
        }

    // Track milestone progress using AI for intelligent progress estimation
    let trackMilestoneProgressWithAI (aiService: IAIService) (milestoneId: Guid) (name: string) (targetDate: DateTime) (startDate: DateTime) (tasks: TaskProgress list) (progressContext: string) : Async<MilestoneProgress> =
        async {
            let tasksText = tasks |> List.map (fun t -> sprintf "task %s: %d%% complete" (string t.TaskId) t.Progress) |> String.concat "; "
            
            let prompt = sprintf "Track milestone: '%s', target %s, start %s, tasks [%s], context '%s'. Return JSON: {\"progress\": number (0-100), \"onTrack\": bool}" name (targetDate.ToString("o")) (startDate.ToString("o")) tasksText progressContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcProgress() = 
                if tasks.IsEmpty then 0
                else
                    let totalProgress = tasks |> List.sumBy (fun t -> t.Progress)
                    totalProgress / List.length tasks
            let progress = try root.GetProperty("progress").GetInt32() with _ -> calcProgress()
            
            let now = DateTime.UtcNow
            let totalDuration = (targetDate - startDate).Days
            let elapsedDays = (now - startDate).Days
            let calcOnTrack() = 
                if totalDuration <= 0 then true
                else
                    let expectedProgress = int ((float elapsedDays / float totalDuration) * 100.0)
                    progress >= expectedProgress - 10
            let onTrack = try root.GetProperty("onTrack").GetBoolean() with _ -> calcOnTrack()
            
            let estimatedCompletion = estimateCompletionDate startDate targetDate progress
            
            return {
                MilestoneId = milestoneId
                Name = name
                TargetDate = targetDate
                CurrentProgress = progress
                EstimatedCompletion = estimatedCompletion
                OnTrack = onTrack
            }
        }

// Milestone Risk Assessment
module MilestoneRiskAssessor =

    type RiskFactor = {
        Name: string
        Severity: string
        Impact: float
    }

    type MilestoneRisk = {
        MilestoneId: Guid
        RiskLevel: string
        RiskFactors: RiskFactor list
        MitigationRecommendations: string list
    }

    // Assess milestone risk based on multiple factors
    let assessRisk (milestoneId: Guid) (targetDate: DateTime) (currentProgress: int) (taskCompletionRate: float) (remainingTasks: int) : MilestoneRisk =
        let now = DateTime.UtcNow
        let daysRemaining = (targetDate - now).Days
        let riskFactors = ResizeArray<RiskFactor>()
        let recommendations = ResizeArray<string>()
        
        // Time risk
        if daysRemaining < 7 && currentProgress < 50 then
            riskFactors.Add({ Name = "Time constraint"; Severity = "High"; Impact = 0.8 })
            recommendations.Add("Consider extending deadline or reducing scope")
        elif daysRemaining < 14 && currentProgress < 70 then
            riskFactors.Add({ Name = "Time constraint"; Severity = "Medium"; Impact = 0.5 })
            recommendations.Add("Monitor progress closely and allocate additional resources")
        
        // Task completion risk
        if taskCompletionRate < 0.5 then
            riskFactors.Add({ Name = "Low task completion rate"; Severity = "High"; Impact = 0.7 })
            recommendations.Add("Review task dependencies and identify blockers")
        elif taskCompletionRate < 0.75 then
            riskFactors.Add({ Name = "Moderate task completion rate"; Severity = "Medium"; Impact = 0.4 })
            recommendations.Add("Focus on completing high-priority tasks")
        
        // Remaining tasks risk
        if remainingTasks > 10 then
            riskFactors.Add({ Name = "High number of remaining tasks"; Severity = "Medium"; Impact = 0.5 })
            recommendations.Add("Consider parallel task execution")
        
        // Overall risk level
        let totalImpact = riskFactors |> List.ofSeq |> List.sumBy (fun r -> r.Impact)
        let riskLevel = 
            if totalImpact > 1.5 then "Critical"
            elif totalImpact > 1.0 then "High"
            elif totalImpact > 0.5 then "Medium"
            else "Low"
        
        {
            MilestoneId = milestoneId
            RiskLevel = riskLevel
            RiskFactors = List.ofSeq riskFactors
            MitigationRecommendations = List.ofSeq recommendations
        }

    // Assess milestone risk using AI for intelligent risk analysis
    let assessRiskWithAI (aiService: IAIService) (milestoneId: Guid) (targetDate: DateTime) (currentProgress: int) (taskCompletionRate: float) (remainingTasks: int) (riskContext: string) : Async<MilestoneRisk> =
        async {
            let now = DateTime.UtcNow
            let daysRemaining = (targetDate - now).Days
            
            let prompt = sprintf "Assess milestone risk: target %s, progress %d%%, task completion %.1f%%, remaining %d tasks, days %d, context '%s'. Return JSON: {\"riskLevel\": \"Critical/High/Medium/Low\", \"recommendations\": [string]}" (targetDate.ToString("o")) currentProgress taskCompletionRate remainingTasks daysRemaining riskContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let riskLevel = try root.GetProperty("riskLevel").GetString() with _ -> "Low"
            let recommendations = try root.GetProperty("recommendations").EnumerateArray() |> Seq.map (fun r -> r.GetString()) |> List.ofSeq with _ -> []
            
            let riskFactors = ResizeArray<RiskFactor>()
            
            if daysRemaining < 7 && currentProgress < 50 then
                riskFactors.Add({ Name = "Time constraint"; Severity = "High"; Impact = 0.8 })
            elif daysRemaining < 14 && currentProgress < 70 then
                riskFactors.Add({ Name = "Time constraint"; Severity = "Medium"; Impact = 0.5 })
            
            if taskCompletionRate < 0.5 then
                riskFactors.Add({ Name = "Low task completion rate"; Severity = "High"; Impact = 0.7 })
            elif taskCompletionRate < 0.75 then
                riskFactors.Add({ Name = "Moderate task completion rate"; Severity = "Medium"; Impact = 0.4 })
            
            if remainingTasks > 10 then
                riskFactors.Add({ Name = "High number of remaining tasks"; Severity = "Medium"; Impact = 0.5 })
            
            return {
                MilestoneId = milestoneId
                RiskLevel = riskLevel
                RiskFactors = List.ofSeq riskFactors
                MitigationRecommendations = recommendations
            }
        }

// Milestone Dependency Analysis
module MilestoneDependencyAnalyzer =

    type Dependency = {
        Predecessor: Guid
        Successor: Guid
        Type: string
    }

    type DependencyChain = {
        Milestones: Guid list
        CriticalPath: bool
        TotalDuration: int
    }

    // Analyze milestone dependencies
    let analyzeDependencies (milestones: Map<Guid, DateTime>) (dependencies: Dependency list) : DependencyChain list =
        let milestoneGroups = 
            milestones
            |> Map.toList
            |> List.map fst
        
        let chains = ResizeArray<DependencyChain>()
        
        for milestoneId in milestoneGroups do
            let predecessors = 
                dependencies
                |> List.filter (fun d -> d.Successor = milestoneId)
                |> List.map (fun d -> d.Predecessor)
            
            let chain = 
                predecessors @ [milestoneId]
                |> List.distinct
            
            let totalDuration = 
                chain
                |> List.sumBy (fun id -> 
                    match Map.tryFind id milestones with
                    | Some date -> 1  // Simplified: each milestone adds 1 day
                    | None -> 0)
            
            chains.Add({
                Milestones = chain
                CriticalPath = chain.Length > 2
                TotalDuration = totalDuration
            })
        
        List.ofSeq chains

    // Identify critical path milestones
    let identifyCriticalPath (chains: DependencyChain list) : Guid list =
        chains
        |> List.filter (fun c -> c.CriticalPath)
        |> List.collect (fun c -> c.Milestones)
        |> List.distinct

    // Analyze dependencies using AI for intelligent dependency management
    let analyzeDependenciesWithAI (aiService: IAIService) (milestones: Map<Guid, DateTime>) (dependencies: Dependency list) (dependencyContext: string) : Async<DependencyChain list> =
        async {
            let milestonesText = milestones |> Map.toList |> List.map (fun (id, date) -> sprintf "%s: %s" (string id) (date.ToString("o"))) |> String.concat "; "
            let depsText = dependencies |> List.map (fun d -> sprintf "%s -> %s (%s)" (string d.Predecessor) (string d.Successor) d.Type) |> String.concat "; "
            
            let prompt = sprintf "Analyze dependencies: milestones [%s], dependencies [%s], context '%s'. Return JSON: {\"criticalMilestoneIds\": [string]}" milestonesText depsText dependencyContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let criticalIds = try root.GetProperty("criticalMilestoneIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> Set.ofSeq with _ -> Set.empty
            
            let milestoneGroups = 
                milestones
                |> Map.toList
                |> List.map fst
            
            let chains = ResizeArray<DependencyChain>()
            
            for milestoneId in milestoneGroups do
                let predecessors = 
                    dependencies
                    |> List.filter (fun d -> d.Successor = milestoneId)
                    |> List.map (fun d -> d.Predecessor)
                
                let chain = 
                    predecessors @ [milestoneId]
                    |> List.distinct
                
                let totalDuration = 
                    chain
                    |> List.sumBy (fun id -> 
                        match Map.tryFind id milestones with
                        | Some date -> 1
                        | None -> 0)
                
                let isCritical = chain |> List.exists (fun id -> Set.contains id criticalIds)
                
                chains.Add({
                    Milestones = chain
                    CriticalPath = isCritical
                    TotalDuration = totalDuration
                })
            
            return List.ofSeq chains
        }
