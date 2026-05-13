namespace Libr4.Projects.Domain.Gantt.Algorithms

open System
open System.Text.Json
open Libr4.AI.Application.Abstractions

// Shared types for Gantt algorithms
type TaskNode = {
    Id: Guid
    Name: string
    Duration: int
    EarliestStart: int
    EarliestFinish: int
    LatestStart: int
    LatestFinish: int
    Slack: int
    AssignedToId: string option
}

type Dependency = {
    Predecessor: Guid
    Successor: Guid
    Type: string
    Lag: int
}

// Critical Path Analysis
module CriticalPathAnalyzer =

    // Calculate earliest start and finish times (forward pass)
    let forwardPass (tasks: TaskNode list) (dependencies: Dependency list) : TaskNode list =
        let taskMap = tasks |> List.map (fun t -> (t.Id, t)) |> Map.ofList
        
        let rec calculateEarliest (taskId: Guid) (visited: Set<Guid>) : int * int =
            if Set.contains taskId visited then
                failwith "Circular dependency detected"
            
            let predecessors = 
                dependencies
                |> List.filter (fun d -> d.Successor = taskId)
                |> List.map (fun d -> d.Predecessor)
            
            let newVisited = Set.add taskId visited
            
            if predecessors.IsEmpty then
                (0, (Map.find taskId taskMap).Duration)
            else
                let predecessorTimes = 
                    predecessors
                    |> List.map (fun predId ->
                        let (start, finish) = calculateEarliest predId newVisited
                        let dep = dependencies |> List.find (fun d -> d.Predecessor = predId && d.Successor = taskId)
                        finish + dep.Lag)
                
                let maxPredecessorTime = if predecessorTimes.IsEmpty then 0 else List.max predecessorTimes
                let duration = (Map.find taskId taskMap).Duration
                (maxPredecessorTime, maxPredecessorTime + duration)
        
        tasks
        |> List.map (fun t ->
            let (earliestStart, earliestFinish) = calculateEarliest t.Id Set.empty
            { t with EarliestStart = earliestStart; EarliestFinish = earliestFinish })

    // Calculate latest start and finish times (backward pass)
    let backwardPass (tasks: TaskNode list) (dependencies: Dependency list) : TaskNode list =
        let taskMap = tasks |> List.map (fun t -> (t.Id, t)) |> Map.ofList
        let projectEnd = tasks |> List.map (fun t -> t.EarliestFinish) |> List.max
        
        let rec calculateLatest (taskId: Guid) (visited: Set<Guid>) : int * int =
            if Set.contains taskId visited then
                failwith "Circular dependency detected"
            
            let successors = 
                dependencies
                |> List.filter (fun d -> d.Predecessor = taskId)
                |> List.map (fun d -> d.Successor)
            
            let newVisited = Set.add taskId visited
            
            if successors.IsEmpty then
                (projectEnd - (Map.find taskId taskMap).Duration, projectEnd)
            else
                let successorTimes = 
                    successors
                    |> List.map (fun succId ->
                        let (start, finish) = calculateLatest succId newVisited
                        let dep = dependencies |> List.find (fun d -> d.Predecessor = taskId && d.Successor = succId)
                        start - dep.Lag)
                
                let minSuccessorTime = if successorTimes.IsEmpty then projectEnd else List.min successorTimes
                let duration = (Map.find taskId taskMap).Duration
                (minSuccessorTime - duration, minSuccessorTime)
        
        tasks
        |> List.map (fun t ->
            let (latestStart, latestFinish) = calculateLatest t.Id Set.empty
            let slack = latestStart - t.EarliestStart
            { t with LatestStart = latestStart; LatestFinish = latestFinish; Slack = slack })

    // Identify critical path (tasks with zero slack)
    let identifyCriticalPath (tasks: TaskNode list) : Guid list =
        tasks
        |> List.filter (fun t -> t.Slack = 0)
        |> List.map (fun t -> t.Id)

    // Identify critical path using AI for intelligent risk analysis
    let identifyCriticalPathWithAI (aiService: IAIService) (tasks: TaskNode list) (projectContext: string) : Async<Guid list> =
        async {
            let tasksText = tasks |> List.map (fun t -> sprintf "%s (duration %d, slack %d)" t.Name t.Duration t.Slack) |> String.concat "; "
            
            let prompt = sprintf "Identify critical path: tasks [%s], context '%s'. Return JSON: {\"criticalTaskIds\": [string]}" tasksText projectContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcCriticalIds() = tasks |> List.filter (fun t -> t.Slack = 0) |> List.map (fun t -> t.Id)
            let criticalIds = try root.GetProperty("criticalTaskIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> List.ofSeq with _ -> calcCriticalIds()
            
            return criticalIds
        }

// Schedule Optimization
module ScheduleOptimizer =

    type ResourceAllocation = {
        TaskId: Guid
        ResourceId: Guid
        AssignedDate: DateTime
        Hours: float
    }

    type ResourceCapacity = {
        ResourceId: Guid
        DailyCapacity: float
        AssignedHours: float
    }

    // Optimize schedule based on resource constraints
    let optimizeForResourceConstraints (tasks: TaskNode list) (resources: ResourceCapacity list) : TaskNode list =
        let resourceMap = resources |> List.map (fun r -> (r.ResourceId, r)) |> Map.ofList
        
        // Simple optimization: delay tasks that exceed resource capacity
        let mutable optimizedTasks = tasks
        let mutable currentDate = DateTime.UtcNow
        
        for task in tasks do
            let resource = 
                match task.AssignedToId with
                | Some assignedId when assignedId <> "" ->
                    resources 
                    |> List.tryFind (fun r -> r.ResourceId = Guid.Parse(assignedId))
                | _ -> None
            
            match resource with
            | Some res ->
                if res.AssignedHours > res.DailyCapacity then
                    let delayDays = int (Math.Ceiling(res.AssignedHours / res.DailyCapacity))
                    let delayedTask = { task with EarliestStart = task.EarliestStart + delayDays; EarliestFinish = task.EarliestFinish + delayDays }
                    optimizedTasks <- optimizedTasks |> List.map (fun t -> if t.Id = task.Id then delayedTask else t)
            | None -> ()
        
        optimizedTasks

    // Optimize schedule using AI for intelligent resource management
    let optimizeForResourceConstraintsWithAI (aiService: IAIService) (tasks: TaskNode list) (resources: ResourceCapacity list) (scheduleContext: string) : Async<TaskNode list> =
        async {
            let tasksText = tasks |> List.map (fun t -> sprintf "%s (duration %d, assigned %O)" t.Name t.Duration t.AssignedToId) |> String.concat "; "
            let resourcesText = resources |> List.map (fun r -> sprintf "%s: capacity %.1f, assigned %.1f" (string r.ResourceId) r.DailyCapacity r.AssignedHours) |> String.concat "; "
            
            let prompt = sprintf "Optimize schedule: tasks [%s], resources [%s], context '%s'. Return JSON: {\"optimizedTaskIds\": [string], \"delays\": [number]}" tasksText resourcesText scheduleContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let optimizedIds = try root.GetProperty("optimizedTaskIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> Set.ofSeq with _ -> Set.empty
            let delays = try root.GetProperty("delays").EnumerateArray() |> Seq.map (fun d -> d.GetInt32()) |> List.ofSeq with _ -> []
            
            let optimizedTasks = 
                tasks
                |> List.mapi (fun i task ->
                    if Set.contains task.Id optimizedIds && i < List.length delays then
                        { task with EarliestStart = task.EarliestStart + delays.[i]; EarliestFinish = task.EarliestFinish + delays.[i] }
                    else task)
            
            return optimizedTasks
        }

// Resource Leveling
module ResourceLeveler =

    type ResourceAssignment = {
        ResourceId: Guid
        TaskId: Guid
        StartDate: DateTime
        EndDate: DateTime
    }

    // Level resources to avoid overallocation
    let levelResources (assignments: ResourceAssignment list) (dailyCapacity: float) : ResourceAssignment list =
        let resourceGroups = 
            assignments
            |> List.groupBy (fun a -> a.ResourceId)
        
        let leveledAssignments = ResizeArray<ResourceAssignment>()
        
        for (resourceId, group) in resourceGroups do
            let sortedByStart = group |> List.sortBy (fun a -> a.StartDate)
            
            let mutable currentAssignments = []
            let mutable currentDate = DateTime.MinValue
            
            for assignment in sortedByStart do
                if currentDate = DateTime.MinValue || assignment.StartDate >= currentDate then
                    currentAssignments <- assignment :: currentAssignments
                    currentDate <- assignment.EndDate
                else
                    // Delay assignment to avoid overallocation
                    let delayedAssignment = { assignment with StartDate = currentDate; EndDate = currentDate.AddDays((assignment.EndDate - assignment.StartDate).Days) }
                    currentAssignments <- delayedAssignment :: currentAssignments
                    currentDate <- delayedAssignment.EndDate
            
            leveledAssignments.AddRange(currentAssignments)
        
        List.ofSeq leveledAssignments

    // Level resources using AI for intelligent resource allocation
    let levelResourcesWithAI (aiService: IAIService) (assignments: ResourceAssignment list) (dailyCapacity: float) (levelingContext: string) : Async<ResourceAssignment list> =
        async {
            let assignmentsText = assignments |> List.map (fun a -> sprintf "%s: task %s (from %s to %s)" (string a.ResourceId) (string a.TaskId) (a.StartDate.ToString("o")) (a.EndDate.ToString("o"))) |> String.concat "; "
            
            let prompt = sprintf "Level resources: assignments [%s], capacity %.1f, context '%s'. Return JSON: {\"leveledTaskIds\": [string], \"newStartDays\": [number]}" assignmentsText dailyCapacity levelingContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let leveledIds = try root.GetProperty("leveledTaskIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> Set.ofSeq with _ -> Set.empty
            let newStartDays = try root.GetProperty("newStartDays").EnumerateArray() |> Seq.map (fun d -> d.GetInt32()) |> List.ofSeq with _ -> []
            
            let leveledAssignments = 
                assignments
                |> List.mapi (fun i assignment ->
                    if Set.contains assignment.TaskId leveledIds && i < List.length newStartDays then
                        let newStart = assignment.StartDate.AddDays(float newStartDays.[i])
                        { assignment with StartDate = newStart; EndDate = newStart.AddDays((assignment.EndDate - assignment.StartDate).Days) }
                    else assignment)
            
            return leveledAssignments
        }

// Milestone Tracking
module MilestoneTracker =

    type Milestone = {
        Id: Guid
        Name: string
        TargetDate: DateTime
        Status: string
        CompletionPercentage: float
    }

    // Calculate milestone progress based on task completion
    let calculateMilestoneProgress (milestoneTasks: Guid list) (taskProgress: Map<Guid, int>) : float =
        if milestoneTasks.IsEmpty then 0.0
        else
            let totalProgress = 
                milestoneTasks
                |> List.sumBy (fun taskId ->
                    Map.tryFind taskId taskProgress |> Option.defaultValue 0)
            
            float totalProgress / float milestoneTasks.Length

    // Identify milestones at risk
    let identifyMilestonesAtRisk (milestones: Milestone list) : Milestone list =
        let now = DateTime.UtcNow
        milestones
        |> List.filter (fun m ->
            m.Status <> "Completed" &&
            m.TargetDate < now.AddDays(7.0) &&
            m.CompletionPercentage < 50.0)
        |> List.sortBy (fun m -> m.TargetDate)

    // Identify milestones at risk using AI for intelligent risk assessment
    let identifyMilestonesAtRiskWithAI (aiService: IAIService) (milestones: Milestone list) (riskContext: string) : Async<Milestone list> =
        async {
            let milestonesText = milestones |> List.map (fun m -> sprintf "%s: target %s, status '%s', progress %.1f%%" m.Name (m.TargetDate.ToString("o")) m.Status m.CompletionPercentage) |> String.concat "; "
            
            let prompt = sprintf "Identify milestones at risk: milestones [%s], context '%s'. Return JSON: {\"atRiskMilestoneIds\": [string]}" milestonesText riskContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let atRiskIds = try root.GetProperty("atRiskMilestoneIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> Set.ofSeq with _ -> Set.empty
            
            let atRiskMilestones = 
                milestones
                |> List.filter (fun m -> Set.contains m.Id atRiskIds)
                |> List.sortBy (fun m -> m.TargetDate)
            
            return atRiskMilestones
        }
