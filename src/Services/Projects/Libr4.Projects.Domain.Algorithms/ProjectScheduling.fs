namespace Libr4.Projects.Domain.Algorithms

open System

// Dependency Types (must be defined before use)
type DependencyType = FinishToStart = 0 | StartToStart = 1 | FinishToFinish = 2 | StartToFinish = 3

// Task Scheduling Algorithms
module TaskScheduler =

    // Simple record for task scheduling
    type ScheduledTask = {
        Id: Guid
        Duration: int
        Dependencies: Guid list
        EarliestStart: int
        EarliestFinish: int
        LatestStart: int
        LatestFinish: int
        Slack: int
        IsCritical: bool
    }

    // Record for task data
    type TaskData = {
        Id: Guid
        Duration: int
        Dependencies: Guid list
    }

    // Calculate critical path using CPM (Critical Path Method)
    let calculateCriticalPath (tasks: TaskData list) : ScheduledTask list =
        // Create a map for quick lookup
        let taskMap = tasks |> List.map (fun t -> t.Id, t) |> Map.ofList
        
        // Forward pass - calculate earliest start/finish
        let rec forwardPass (visited: Set<Guid>) (taskId: Guid) (currentStart: int) : (Guid * int * int) =
            if visited.Contains taskId then (taskId, currentStart, currentStart)
            else
                let taskData = match taskMap.TryFind taskId with Some t -> t | None -> { Id = taskId; Duration = 0; Dependencies = [] }
                let dependencies = taskData.Dependencies
                
                let mutable maxDepFinish = 0
                for depId in dependencies do
                    let (_, depStart, depFinish) = forwardPass visited depId 0
                    if depFinish > maxDepFinish then maxDepFinish <- depFinish
                
                let earliestStart = max currentStart maxDepFinish
                let earliestFinish = earliestStart + taskData.Duration
                (taskId, earliestStart, earliestFinish)
        
        let forwardResults = 
            tasks
            |> List.map (fun t -> forwardPass Set.empty t.Id 0)
        
        // Find project duration
        let projectDuration = forwardResults |> List.map (fun (_, _, finish) -> finish) |> List.max
        
        // Backward pass - calculate latest start/finish
        let taskMapWithForward = 
            forwardResults
            |> List.fold (fun map (id, start, finish) ->
                match Map.tryFind id taskMap with
                | Some taskData -> Map.add id (taskData, start, finish) map
                | None -> map) Map.empty
        
        let rec backwardPass (visited: Set<Guid>) (taskId: Guid) (currentFinish: int) : (Guid * int * int) =
            if visited.Contains taskId then (taskId, currentFinish, currentFinish)
            else
                match Map.tryFind taskId taskMapWithForward with
                | Some (taskData, _, _) ->
                    let latestFinish = currentFinish
                    let latestStart = latestFinish - taskData.Duration
                    (taskId, latestStart, latestFinish)
                | None -> (taskId, currentFinish, currentFinish)
        
        let backwardResults = 
            taskMapWithForward
            |> Map.toList
            |> List.map (fun (id, (taskData, start, finish)) ->
                let (_, latestStart, latestFinish) = backwardPass Set.empty id projectDuration
                (id, latestStart, latestFinish))
        
        // Calculate slack and identify critical path
        let scheduledTasks = 
            forwardResults
            |> List.zip backwardResults
            |> List.map (fun ((id, latestStart, latestFinish), (_, earliestStart, earliestFinish)) ->
                let slack = latestStart - earliestStart
                let taskData = match Map.tryFind id taskMap with Some t -> t | None -> { Id = id; Duration = 0; Dependencies = [] }
                {
                    Id = id
                    Duration = taskData.Duration
                    Dependencies = taskData.Dependencies
                    EarliestStart = earliestStart
                    EarliestFinish = earliestFinish
                    LatestStart = latestStart
                    LatestFinish = latestFinish
                    Slack = slack
                    IsCritical = slack = 0
                })
        
        scheduledTasks

    // Calculate project duration from scheduled tasks
    let calculateProjectDuration (tasks: ScheduledTask list) : int =
        match tasks with
        | [] -> 0
        | _ -> tasks |> List.map (fun t -> t.EarliestFinish) |> List.max

    // Calculate resource utilization
    let calculateResourceUtilization (assignments: (Guid * int) list) (totalResources: int) : float =
        if totalResources = 0 then 0.0
        else
            let totalAssigned = assignments |> List.sumBy snd
            float totalAssigned / float totalResources * 100.0

// Resource Allocation Algorithms
module ResourceAllocator =

    type ResourceAssignment = {
        TaskId: Guid
        ResourceId: Guid
        Hours: int
        StartDate: DateTime
        EndDate: DateTime
    }

    // Allocate resources based on task priority and availability
    let allocateResources (tasks: (Guid * int * int) list) (resourceAvailability: Map<Guid, int>) : ResourceAssignment list =
        let prioritizedTasks = 
            tasks
            |> List.sortByDescending (fun (_, priority, _) -> priority)
        
        let mutable assignments: ResourceAssignment list = []
        let mutable usedResources: Map<Guid, int> = Map.empty
        
        for (taskId, priority, hours) in prioritizedTasks do
            match resourceAvailability |> Map.tryFind (Guid.NewGuid()) with // Simplified - would use actual resource IDs
            | Some available ->
                let used = match Map.tryFind (Guid.NewGuid()) usedResources with Some u -> u | None -> 0
                let remaining = available - used
                if remaining >= hours then
                    usedResources <- Map.add (Guid.NewGuid()) (used + hours) usedResources
                    assignments <- 
                        {
                            TaskId = taskId
                            ResourceId = Guid.NewGuid()
                            Hours = hours
                            StartDate = DateTime.UtcNow
                            EndDate = DateTime.UtcNow.AddDays(float hours / 8.0)
                        } :: assignments
            | None -> ()
        
        assignments |> List.rev

    // Calculate resource conflicts
    let calculateResourceConflicts (assignments: ResourceAssignment list) : (Guid * int) list =
        assignments
        |> List.groupBy (fun a -> a.ResourceId)
        |> List.map (fun (resourceId, assigns) ->
            let totalHours = assigns |> List.sumBy (fun a -> a.Hours)
            (resourceId, totalHours))
        |> List.filter (fun (_, hours) -> hours > 40) // Assuming 40 hours per week

// Project Metrics Algorithms
module ProjectMetrics =

    // Calculate project health score (0-100)
    let calculateProjectHealth (budget: decimal) (spent: decimal) (progress: float) (deadline: DateTime option) : float =
        let budgetHealth = if budget = 0m then 100.0 else float (spent / budget * 100m)
        let progressHealth = progress
        let deadlineHealth = 
            match deadline with
            | Some d ->
                let remainingDays = (d - DateTime.UtcNow).Days
                if remainingDays < 0 then 0.0
                elif remainingDays < 7 then 50.0
                elif remainingDays < 30 then 75.0
                else 100.0
            | None -> 100.0
        
        (budgetHealth + progressHealth + deadlineHealth) / 3.0

    // Calculate team velocity (tasks completed per sprint)
    let calculateTeamVelocity (completedTasks: int list) : float =
        match completedTasks with
        | [] -> 0.0
        | _ -> float (List.sum completedTasks) / float (List.length completedTasks)

    // Calculate burn rate (spending per week)
    let calculateBurnRate (spent: decimal) (weeksElapsed: int) : decimal =
        if weeksElapsed = 0 then 0m
        else spent / decimal weeksElapsed

    // Estimate completion date based on velocity
    let estimateCompletionDate (remainingTasks: int) (velocity: float) (startDate: DateTime) : DateTime =
        if velocity = 0.0 then DateTime.MaxValue
        else
            let weeksNeeded = float remainingTasks / velocity
            startDate.AddDays(weeksNeeded * 7.0)
