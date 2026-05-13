namespace Libr4.Projects.Domain.Workflows.Algorithms

open System
open System.Text.Json
open Libr4.AI.Application.Abstractions

// Shared types for workflow algorithms
type StepResult = {
    StepId: Guid
    Status: string
    Result: string option
    Duration: TimeSpan
}

type ExecutionPlan = {
    WorkflowId: Guid
    Steps: Guid list
    Dependencies: Map<Guid, Guid list>
}

// Workflow Execution Engine
module WorkflowEngine =

    // Execute workflow step by step
    let executeWorkflow (steps: Guid list) (dependencies: Map<Guid, Guid list>) (executeStep: Guid -> StepResult) : StepResult list =
        let executed = System.Collections.Generic.Dictionary<Guid, StepResult>()
        let results = ResizeArray<StepResult>()
        
        let canExecute (stepId: Guid) =
            match Map.tryFind stepId dependencies with
            | None -> true
            | Some deps ->
                deps |> List.forall (fun depId -> executed.ContainsKey(depId))
        
        let mutable remainingSteps = steps
        while not (List.isEmpty remainingSteps) do
            let executable = remainingSteps |> List.filter canExecute
            
            if executable.IsEmpty then
                // Circular dependency or missing dependency
                failwith "Cannot execute workflow: circular or missing dependency"
            else
                for stepId in executable do
                    let result = executeStep stepId
                    executed.[stepId] <- result
                    results.Add(result)
                
                remainingSteps <- remainingSteps |> List.filter (fun id -> not (List.contains id executable))
        
        List.ofSeq results

    // Calculate critical path in workflow
    let calculateCriticalPath (steps: Guid list) (durations: Map<Guid, TimeSpan>) (dependencies: Map<Guid, Guid list>) : Guid list =
        let rec longestPath (current: Guid) (visited: Set<Guid>) : Guid list =
            if Set.contains current visited then
                [current]
            else
                let newVisited = Set.add current visited
                let deps = Map.tryFind current dependencies |> Option.defaultValue []
                
                if deps.IsEmpty then
                    [current]
                else
                    let paths = deps |> List.map (fun dep -> longestPath dep newVisited)
                    let bestPath = paths |> List.maxBy (fun path -> 
                        path |> List.sumBy (fun id -> 
                            Map.tryFind id durations |> Option.defaultValue TimeSpan.Zero |> fun d -> d.TotalMilliseconds))
                    current :: bestPath
        
        let paths = steps |> List.map (fun step -> longestPath step Set.empty)
        paths |> List.maxBy (fun path -> 
            path |> List.sumBy (fun id -> 
                Map.tryFind id durations |> Option.defaultValue TimeSpan.Zero |> fun d -> d.TotalMilliseconds))

    // Calculate critical path using AI for intelligent path analysis
    let calculateCriticalPathWithAI (aiService: IAIService) (steps: Guid list) (durations: Map<Guid, TimeSpan>) (dependencies: Map<Guid, Guid list>) (workflowContext: string) : Async<Guid list> =
        async {
            let stepsText = steps |> List.map (fun id -> sprintf "%s: %.1fms" (string id) (Map.tryFind id durations |> Option.defaultValue TimeSpan.Zero |> fun d -> d.TotalMilliseconds)) |> String.concat "; "
            let depsText = dependencies |> Map.toList |> List.map (fun (id, deps) -> sprintf "%s -> %A" (string id) deps) |> String.concat "; "
            
            let prompt = sprintf "Calculate critical path: steps [%s], dependencies [%s], context '%s'. Return JSON: {\"criticalStepIds\": [string]}" stepsText depsText workflowContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let criticalIds = try root.GetProperty("criticalStepIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> Set.ofSeq with _ -> Set.empty
            
            if criticalIds.IsEmpty then
                return calculateCriticalPath steps durations dependencies
            else
                return steps |> List.filter (fun id -> Set.contains id criticalIds)
        }

// Workflow Optimization
module WorkflowOptimizer =

    type PerformanceMetrics = {
        AverageExecutionTime: TimeSpan
        SuccessRate: float
        BottleneckSteps: Guid list
    }

    // Analyze workflow performance
    let analyzePerformance (executions: StepResult list list) : PerformanceMetrics =
        if executions.IsEmpty then
            {
                AverageExecutionTime = TimeSpan.Zero
                SuccessRate = 0.0
                BottleneckSteps = []
            }
        else
            let allResults = executions |> List.collect id
            let totalDuration = allResults |> List.sumBy (fun r -> r.Duration.TotalMilliseconds)
            let averageTime = TimeSpan.FromMilliseconds(totalDuration / float allResults.Length)
            
            let successCount = allResults |> List.filter (fun r -> r.Status = "Completed") |> List.length
            let successRate = float successCount / float allResults.Length
            
            // Identify bottleneck steps (slowest steps)
            let stepDurations = 
                allResults
                |> List.groupBy (fun r -> r.StepId)
                |> List.map (fun (stepId, results) -> 
                    let avgDuration = results |> List.averageBy (fun r -> r.Duration.TotalMilliseconds)
                    (stepId, avgDuration))
            
            let avgStepDuration = stepDurations |> List.averageBy snd
            let bottlenecks = 
                stepDurations
                |> List.filter (fun (_, duration) -> duration > avgStepDuration * 1.5)
                |> List.map fst
            
            {
                AverageExecutionTime = averageTime
                SuccessRate = successRate
                BottleneckSteps = bottlenecks
            }

    // Suggest workflow improvements
    let suggestImprovements (metrics: PerformanceMetrics) : string list =
        let suggestions = 
            if metrics.SuccessRate < 0.8 then
                ["Improve error handling and retry logic for failed steps"]
            else
                []
        
        let suggestions2 = 
            if not metrics.BottleneckSteps.IsEmpty then
                [sprintf "Optimize bottleneck steps: %A" metrics.BottleneckSteps]
            else
                []
        
        suggestions @ suggestions2

    // Analyze performance using AI for intelligent workflow optimization
    let analyzePerformanceWithAI (aiService: IAIService) (executions: StepResult list list) (optimizationContext: string) : Async<PerformanceMetrics> =
        async {
            let allResults = executions |> List.collect id
            let totalDuration = allResults |> List.sumBy (fun r -> r.Duration.TotalMilliseconds)
            let averageTime = TimeSpan.FromMilliseconds(totalDuration / float allResults.Length)
            let successCount = allResults |> List.filter (fun r -> r.Status = "Completed") |> List.length
            let successRate = float successCount / float allResults.Length
            
            let resultsText = allResults |> List.map (fun r -> sprintf "%s: %s (%.1fms)" (string r.StepId) r.Status r.Duration.TotalMilliseconds) |> String.concat "; "
            
            let prompt = sprintf "Analyze workflow: avg %.1fms, success %.1f%%, results [%s], context '%s'. Return JSON: {\"bottleneckStepIds\": [string]}" averageTime.TotalMilliseconds (successRate * 100.0) resultsText optimizationContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let bottleneckIds = try root.GetProperty("bottleneckStepIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> List.ofSeq with _ -> []
            
            return {
                AverageExecutionTime = averageTime
                SuccessRate = successRate
                BottleneckSteps = bottleneckIds
            }
        }

// Workflow Validation
module WorkflowValidator =

    type ValidationError = {
        StepId: Guid option
        Message: string
        Severity: string
    }

    // Validate workflow structure
    let validateWorkflow (steps: Guid list) (dependencies: Map<Guid, Guid list>) : ValidationError list =
        let errors = ResizeArray<ValidationError>()
        
        // Check for circular dependencies
        let rec hasCycle (current: Guid) (visited: Set<Guid>) (path: Guid list) : bool =
            if Set.contains current visited then
                List.contains current path
            else
                let newVisited = Set.add current visited
                let newPath = current :: path
                let deps = Map.tryFind current dependencies |> Option.defaultValue []
                deps |> List.exists (fun dep -> hasCycle dep newVisited newPath)
        
        for stepId in steps do
            if hasCycle stepId Set.empty [] then
                errors.Add({
                    StepId = Some stepId
                    Message = sprintf "Circular dependency detected for step %s" (stepId.ToString())
                    Severity = "Error"
                })
        
        // Check for orphan steps (no dependencies and no dependents)
        let allReferenced = 
            dependencies
            |> Map.toList
            |> List.collect snd
            |> Set.ofList
        
        let orphanSteps = steps |> List.filter (fun id -> not (Set.contains id allReferenced))
        for stepId in orphanSteps do
            errors.Add({
                StepId = Some stepId
                Message = sprintf "Orphan step detected: %s has no dependencies or dependents" (stepId.ToString())
                Severity = "Warning"
            })
        
        List.ofSeq errors

    // Validate workflow using AI for intelligent validation
    let validateWorkflowWithAI (aiService: IAIService) (steps: Guid list) (dependencies: Map<Guid, Guid list>) (validationContext: string) : Async<ValidationError list> =
        async {
            let stepsText = steps |> List.map (fun id -> string id) |> String.concat "; "
            let depsText = dependencies |> Map.toList |> List.map (fun (id, deps) -> sprintf "%s -> %A" (string id) deps) |> String.concat "; "
            
            let prompt = sprintf "Validate workflow: steps [%s], dependencies [%s], context '%s'. Return JSON: {\"errorStepIds\": [string], \"warningStepIds\": [string]}" stepsText depsText validationContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "projects") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let errorIds = try root.GetProperty("errorStepIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> Set.ofSeq with _ -> Set.empty
            let warningIds = try root.GetProperty("warningStepIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> Set.ofSeq with _ -> Set.empty
            
            let errors = ResizeArray<ValidationError>()
            
            for stepId in steps do
                if Set.contains stepId errorIds then
                    errors.Add({
                        StepId = Some stepId
                        Message = sprintf "Validation error for step %s" (stepId.ToString())
                        Severity = "Error"
                    })
                elif Set.contains stepId warningIds then
                    errors.Add({
                        StepId = Some stepId
                        Message = sprintf "Validation warning for step %s" (stepId.ToString())
                        Severity = "Warning"
                    })
            
            if errors.Count = 0 then
                return validateWorkflow steps dependencies
            else
                return List.ofSeq errors
        }
