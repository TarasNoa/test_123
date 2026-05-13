namespace Libr4.DevOps.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Application.Abstractions

// Pipeline Orchestration Algorithms
module PipelineOrchestrator =

    type PipelineExecutionPlan = {
        Stages: string list
        Parallelizable: bool list
        EstimatedDuration: int
    }

    // Calculate optimal pipeline execution plan
    let calculateExecutionPlan (stages: (string * int * bool) list) : PipelineExecutionPlan =
        let stageNames = stages |> List.map (fun (name, _, _) -> name)
        let parallelizable = stages |> List.map (fun (_, _, parallel) -> parallel)
        let estimatedDuration = stages |> List.sumBy (fun (_, duration, _) -> duration)
        {
            Stages = stageNames
            Parallelizable = parallelizable
            EstimatedDuration = estimatedDuration
        }

    // Calculate pipeline duration considering parallel stages
    let calculateActualDuration (stages: (string * int * bool) list) : int =
        let mutable maxParallelDuration = 0
        let mutable sequentialDuration = 0
        
        for (_, duration, parallel) in stages do
            if parallel then
                if duration > maxParallelDuration then maxParallelDuration <- duration
            else
                sequentialDuration <- sequentialDuration + duration
        
        sequentialDuration + maxParallelDuration

    // Calculate execution plan using AI for intelligent pipeline optimization
    let calculateExecutionPlanWithAI (aiService: IAIService) (stages: (string * int * bool) list) (pipelineContext: string) : Async<PipelineExecutionPlan> =
        async {
            let stagesText = stages |> List.map (fun (name, duration, parallel) -> sprintf "%s: %d min, parallel %b" name duration parallel) |> String.concat "; "
            
            let prompt = sprintf "Optimize pipeline execution: stages [%s], context '%s'. Return JSON: {\"stages\": [string], \"parallelizable\": [bool], \"estimatedDuration\": number}" stagesText pipelineContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "devops") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcStagesAI() = stages |> List.map (fun (name, _, _) -> name)
            let stagesAI = try root.GetProperty("stages").EnumerateArray() |> Seq.map (fun s -> s.GetString()) |> List.ofSeq with _ -> calcStagesAI()
            
            let calcParallelizableAI() = stages |> List.map (fun (_, _, parallel) -> parallel)
            let parallelizableAI = try root.GetProperty("parallelizable").EnumerateArray() |> Seq.map (fun p -> p.GetBoolean()) |> List.ofSeq with _ -> calcParallelizableAI()
            
            let calcEstimatedDurationAI() = stages |> List.sumBy (fun (_, duration, _) -> duration)
            let estimatedDurationAI = try root.GetProperty("estimatedDuration").GetInt32() with _ -> calcEstimatedDurationAI()
            
            return {
                Stages = stagesAI
                Parallelizable = parallelizableAI
                EstimatedDuration = estimatedDurationAI
            }
        }

// Health Check Algorithms
module HealthChecker =

    type HealthStatus = Healthy | Degraded | Unhealthy
    type HealthMetric = { Name: string; Value: float; Threshold: float; IsCritical: bool }

    // Determine overall health status
    let determineHealthStatus (metrics: HealthMetric list) : HealthStatus =
        let criticalFailures = metrics |> List.filter (fun m -> m.IsCritical && m.Value > m.Threshold) |> List.length
        let totalFailures = metrics |> List.filter (fun m -> m.Value > m.Threshold) |> List.length
        
        match criticalFailures, totalFailures with
        | 0, 0 -> Healthy
        | 0, _ when totalFailures <= 2 -> Degraded
        | 0, _ -> Unhealthy
        | _ -> Unhealthy

    // Calculate health score (0-100)
    let calculateHealthScore (metrics: HealthMetric list) : float =
        match metrics with
        | [] -> 100.0
        | _ ->
            let totalScore = 
                metrics
                |> List.map (fun m ->
                    if m.Value <= m.Threshold then 100.0
                    elif m.IsCritical then 0.0
                    else max 0.0 (100.0 - (m.Value - m.Threshold) * 10.0))
                |> List.sum
            totalScore / float (List.length metrics)

    // Determine health status using AI for intelligent health assessment
    let determineHealthStatusWithAI (aiService: IAIService) (metrics: HealthMetric list) (healthContext: string) : Async<HealthStatus> =
        async {
            let metricsText = metrics |> List.map (fun m -> sprintf "%s: %.2f/%.2f (critical %b)" m.Name m.Value m.Threshold m.IsCritical) |> String.concat "; "
            
            let prompt = sprintf "Determine health status: metrics [%s], context '%s'. Return JSON: {\"status\": \"Healthy/Degraded/Unhealthy\", \"healthScore\": number (0-100)}" metricsText healthContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "devops") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let criticalFailures = metrics |> List.filter (fun m -> m.IsCritical && m.Value > m.Threshold) |> List.length
            let totalFailures = metrics |> List.filter (fun m -> m.Value > m.Threshold) |> List.length
            let calcStatusStr() = 
                match criticalFailures, totalFailures with
                | 0, 0 -> "Healthy"
                | 0, _ when totalFailures <= 2 -> "Degraded"
                | _ -> "Unhealthy"
            let statusStr = try root.GetProperty("status").GetString() with _ -> calcStatusStr()
            
            let status = 
                match statusStr with
                | "Degraded" -> Degraded
                | "Unhealthy" -> Unhealthy
                | _ -> Healthy
            
            return status
        }

// Resource Monitoring Algorithms
module ResourceMonitor =

    type ResourceUsage = {
        CpuPercent: float
        MemoryPercent: float
        DiskPercent: float
        NetworkInBytes: int64
        NetworkOutBytes: int64
    }

    // Detect resource anomalies
    let detectResourceAnomaly (current: ResourceUsage) (baseline: ResourceUsage) (thresholdMultiplier: float) : bool =
        let cpuAnomaly = current.CpuPercent > baseline.CpuPercent * thresholdMultiplier
        let memoryAnomaly = current.MemoryPercent > baseline.MemoryPercent * thresholdMultiplier
        let diskAnomaly = current.DiskPercent > baseline.DiskPercent * thresholdMultiplier
        
        cpuAnomaly || memoryAnomaly || diskAnomaly

    // Calculate resource efficiency score
    let calculateEfficiencyScore (usage: ResourceUsage) : float =
        // Optimal CPU usage is 60-80%, Memory 60-80%
        let cpuScore = 
            if usage.CpuPercent >= 60.0 && usage.CpuPercent <= 80.0 then 100.0
            elif usage.CpuPercent < 60.0 then 50.0 + (usage.CpuPercent / 60.0) * 50.0
            else max 0.0 (100.0 - (usage.CpuPercent - 80.0) * 5.0)
        
        let memoryScore = 
            if usage.MemoryPercent >= 60.0 && usage.MemoryPercent <= 80.0 then 100.0
            elif usage.MemoryPercent < 60.0 then 50.0 + (usage.MemoryPercent / 60.0) * 50.0
            else max 0.0 (100.0 - (usage.MemoryPercent - 80.0) * 5.0)
        
        (cpuScore + memoryScore) / 2.0

    // Detect resource anomaly using AI for intelligent anomaly detection
    let detectResourceAnomalyWithAI (aiService: IAIService) (current: ResourceUsage) (baseline: ResourceUsage) (thresholdMultiplier: float) (resourceContext: string) : Async<bool> =
        async {
            let currentText = sprintf "CPU %.1f%%, Memory %.1f%%, Disk %.1f%%" current.CpuPercent current.MemoryPercent current.DiskPercent
            let baselineText = sprintf "CPU %.1f%%, Memory %.1f%%, Disk %.1f%%" baseline.CpuPercent baseline.MemoryPercent baseline.DiskPercent
            
            let prompt = sprintf "Detect resource anomaly: current [%s], baseline [%s], threshold %.1f, context '%s'. Return JSON: {\"isAnomaly\": bool, \"reason\": string}" currentText baselineText thresholdMultiplier resourceContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "devops") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let cpuAnomaly = current.CpuPercent > baseline.CpuPercent * thresholdMultiplier
            let memoryAnomaly = current.MemoryPercent > baseline.MemoryPercent * thresholdMultiplier
            let diskAnomaly = current.DiskPercent > baseline.DiskPercent * thresholdMultiplier
            let calcIsAnomaly() = cpuAnomaly || memoryAnomaly || diskAnomaly
            let isAnomaly = try root.GetProperty("isAnomaly").GetBoolean() with _ -> calcIsAnomaly()
            
            return isAnomaly
        }

// Deployment Algorithms
module DeploymentManager =

    type DeploymentStrategy = RollingUpdate | BlueGreen | Canary
    type DeploymentPlan = {
        Strategy: DeploymentStrategy
        BatchSize: int
        RollbackEnabled: bool
        HealthCheckInterval: int
    }

    // Calculate optimal deployment plan based on risk
    let calculateDeploymentPlan (instanceCount: int) (riskLevel: int) : DeploymentPlan =
        let strategy = 
            match riskLevel with
            | level when level >= 8 -> BlueGreen
            | level when level >= 5 -> Canary
            | _ -> RollingUpdate
        
        let batchSize = 
            match strategy with
            | RollingUpdate -> max 1 (instanceCount / 4)
            | Canary -> max 1 (instanceCount / 10)
            | BlueGreen -> instanceCount
        
        {
            Strategy = strategy
            BatchSize = batchSize
            RollbackEnabled = true
            HealthCheckInterval = 30
        }

    // Calculate deployment duration
    let calculateDeploymentDuration (plan: DeploymentPlan) (instanceCount: int) (deploymentTimePerInstance: int) : int =
        match plan.Strategy with
        | RollingUpdate -> (instanceCount / plan.BatchSize) * deploymentTimePerInstance * plan.BatchSize
        | BlueGreen -> deploymentTimePerInstance * instanceCount * 2 // Deploy to both environments
        | Canary -> deploymentTimePerInstance * (plan.BatchSize + instanceCount) // Canary first, then rest

    // Calculate deployment plan using AI for intelligent deployment strategy
    let calculateDeploymentPlanWithAI (aiService: IAIService) (instanceCount: int) (riskLevel: int) (deploymentContext: string) : Async<DeploymentPlan> =
        async {
            let prompt = sprintf "Calculate deployment plan: %d instances, risk level %d, context '%s'. Return JSON: {\"strategy\": \"RollingUpdate/BlueGreen/Canary\", \"batchSize\": number, \"healthCheckInterval\": number}" instanceCount riskLevel deploymentContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "devops") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcStrategyStr() = 
                match riskLevel with
                | level when level >= 8 -> "BlueGreen"
                | level when level >= 5 -> "Canary"
                | _ -> "RollingUpdate"
            let strategyStr = try root.GetProperty("strategy").GetString() with _ -> calcStrategyStr()
            
            let strategy = 
                match strategyStr with
                | "BlueGreen" -> BlueGreen
                | "Canary" -> Canary
                | _ -> RollingUpdate
            
            let calcBatchSize() = 
                match strategy with
                | RollingUpdate -> max 1 (instanceCount / 4)
                | Canary -> max 1 (instanceCount / 10)
                | BlueGreen -> instanceCount
            let batchSize = try root.GetProperty("batchSize").GetInt32() with _ -> calcBatchSize()
            
            let healthCheckInterval = try root.GetProperty("healthCheckInterval").GetInt32() with _ -> 30
            
            return {
                Strategy = strategy
                BatchSize = batchSize
                RollbackEnabled = true
                HealthCheckInterval = healthCheckInterval
            }
        }

// Log Analysis Algorithms
module LogAnalyzer =

    type LogLevel = Info | Warning | Error | Critical
    type LogEntry = { Timestamp: DateTime; Level: LogLevel; Message: string; Source: string }

    // Detect anomalies in log patterns
    let detectLogAnomalies (logs: LogEntry list) (errorThreshold: int) (timeWindowMinutes: int) : LogEntry list =
        let now = DateTime.UtcNow
        let windowStart = now.AddMinutes(-float timeWindowMinutes)
        
        let recentLogs = logs |> List.filter (fun l -> l.Timestamp >= windowStart)
        let errorLogs = recentLogs |> List.filter (fun l -> l.Level = Error || l.Level = Critical)
        
        if List.length errorLogs > errorThreshold then errorLogs else []

    // Calculate error rate
    let calculateErrorRate (logs: LogEntry list) : float =
        match logs with
        | [] -> 0.0
        | _ ->
            let errorCount = logs |> List.filter (fun l -> l.Level = Error || l.Level = Critical) |> List.length
            float errorCount / float (List.length logs) * 100.0

    // Detect log anomalies using AI for intelligent pattern detection
    let detectLogAnomaliesWithAI (aiService: IAIService) (logs: LogEntry list) (errorThreshold: int) (timeWindowMinutes: int) (logContext: string) : Async<LogEntry list> =
        async {
            let recentLogs = logs |> List.filter (fun l -> (DateTime.UtcNow - l.Timestamp).TotalMinutes <= float timeWindowMinutes)
            let logsText = recentLogs |> List.map (fun l -> sprintf "[%s] %s: %s" (l.Timestamp.ToString("o")) (string l.Level) l.Message) |> String.concat "; "
            
            let prompt = sprintf "Detect log anomalies: logs [%s], threshold %d errors, window %d min, context '%s'. Return JSON: {\"hasAnomaly\": bool, \"anomalyCount\": number}" logsText errorThreshold timeWindowMinutes logContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "devops") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let errorLogs = recentLogs |> List.filter (fun l -> l.Level = Error || l.Level = Critical)
            let calcHasAnomaly() = errorLogs.Length > errorThreshold
            let hasAnomaly = try root.GetProperty("hasAnomaly").GetBoolean() with _ -> calcHasAnomaly()
            
            if hasAnomaly then
                return errorLogs
            else
                return []
        }
