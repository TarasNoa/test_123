namespace Libr4.AI.Domain.Agents.Algorithms

open System
open System.Text.Json
open Libr4.AI.Domain.Agents
open Libr4.AI.Application.Abstractions

// Agent Capability Matcher
module AgentCapabilityMatcher =

    type CapabilityMatch = {
        AgentId: Guid
        MatchScore: float
        MatchedCapabilities: string list
        MissingCapabilities: string list
    }

    let matchCapabilities (agent: Agent) (requiredCapabilities: string list) : CapabilityMatch =
        let agentTools = agent.AllowedTools |> Seq.map (fun t -> t.Name) |> Set.ofSeq
        let requiredSet = Set.ofList requiredCapabilities
        
        let matched = Set.intersect agentTools requiredSet |> Set.toList
        let missing = Set.difference requiredSet agentTools |> Set.toList
        
        let matchScore = 
            if requiredCapabilities.IsEmpty then 1.0
            else float matched.Length / float requiredCapabilities.Length
        
        {
            AgentId = agent.Id
            MatchScore = matchScore
            MatchedCapabilities = matched
            MissingCapabilities = missing
        }

    // Match capabilities using AI for semantic matching
    let matchCapabilitiesWithAI (aiService: IAIService) (agent: Agent) (requiredCapabilities: string list) (taskDescription: string) : Async<CapabilityMatch> =
        async {
            let agentTools = agent.AllowedTools |> Seq.map (fun t -> t.Name) |> String.concat ", "
            let requiredText = String.concat ", " requiredCapabilities
            let prompt = sprintf "Match agent capabilities: agent tools [%s], required [%s], task '%s'. Return JSON: {\"matchScore\": number (0-1), \"matched\": [string], \"missing\": [string]}" agentTools requiredText taskDescription
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "ai") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let matchScore = 
                try root.GetProperty("matchScore").GetSingle() |> float
                with _ ->
                    let agentToolSet = agent.AllowedTools |> Seq.map (fun t -> t.Name) |> Set.ofSeq
                    let requiredSet = Set.ofList requiredCapabilities
                    let matched = Set.intersect agentToolSet requiredSet |> Set.toList
                    if requiredCapabilities.IsEmpty then 1.0
                    else float matched.Length / float requiredCapabilities.Length
            
            let matched = 
                try
                    root.GetProperty("matched").EnumerateArray()
                    |> Seq.map (fun m -> m.GetString())
                    |> List.ofSeq
                with _ ->
                    let agentToolSet = agent.AllowedTools |> Seq.map (fun t -> t.Name) |> Set.ofSeq
                    let requiredSet = Set.ofList requiredCapabilities
                    Set.intersect agentToolSet requiredSet |> Set.toList
            
            let missing = 
                try
                    root.GetProperty("missing").EnumerateArray()
                    |> Seq.map (fun m -> m.GetString())
                    |> List.ofSeq
                with _ ->
                    let agentToolSet = agent.AllowedTools |> Seq.map (fun t -> t.Name) |> Set.ofSeq
                    let requiredSet = Set.ofList requiredCapabilities
                    Set.difference requiredSet agentToolSet |> Set.toList
            
            return {
                AgentId = agent.Id
                MatchScore = matchScore
                MatchedCapabilities = matched
                MissingCapabilities = missing
            }
        }

// Agent Performance Tracker
module AgentPerformanceTracker =

    type PerformanceMetrics = {
        TotalExecutions: int
        SuccessCount: int
        FailureCount: int
        AverageExecutionTime: float
        SuccessRate: float
        LastExecutionTime: DateTimeOffset option
    }

    let calculatePerformance (executionTimes: TimeSpan list) (successCount: int) (failureCount: int) : PerformanceMetrics =
        let totalExecutions = successCount + failureCount
        let successRate = 
            if totalExecutions = 0 then 0.0
            else float successCount / float totalExecutions
        
        let avgExecutionTime = 
            if executionTimes.IsEmpty then 0.0
            else executionTimes |> List.averageBy (fun ts -> ts.TotalMilliseconds)
        
        let lastExecutionTime = 
            if executionTimes.IsEmpty then None
            else Some DateTimeOffset.UtcNow
        
        {
            TotalExecutions = totalExecutions
            SuccessCount = successCount
            FailureCount = failureCount
            AverageExecutionTime = avgExecutionTime
            SuccessRate = successRate
            LastExecutionTime = lastExecutionTime
        }

    // Predict agent performance using AI
    let predictPerformance (aiService: IAIService) (agentId: Guid) (historicalPerformance: PerformanceMetrics) (taskComplexity: string) : Async<PerformanceMetrics> =
        async {
            let prompt = sprintf "Predict agent performance: agent %s, historical success %.2f, avg time %.1fms, task complexity '%s'. Return JSON: {\"predictedSuccessRate\": number (0-1), \"predictedExecutionTime\": number}" (agentId.ToString()) historicalPerformance.SuccessRate historicalPerformance.AverageExecutionTime taskComplexity
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "ai") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let predictedSuccessRate = 
                try root.GetProperty("predictedSuccessRate").GetSingle() |> float
                with _ -> historicalPerformance.SuccessRate
            
            let predictedExecutionTime = 
                try root.GetProperty("predictedExecutionTime").GetDouble()
                with _ -> historicalPerformance.AverageExecutionTime
            
            return {
                TotalExecutions = historicalPerformance.TotalExecutions + 1
                SuccessCount = historicalPerformance.SuccessCount
                FailureCount = historicalPerformance.FailureCount
                AverageExecutionTime = predictedExecutionTime
                SuccessRate = predictedSuccessRate
                LastExecutionTime = Some DateTimeOffset.UtcNow
            }
        }

// Agent Selector
module AgentSelector =

    type SelectionCriteria = {
        AgentType: AgentType
        RequiredCapabilities: string list
        MinSuccessRate: float
        MaxExecutionTime: float
    }

    type SelectionResult = {
        SelectedAgent: Guid option
        Reason: string
        AlternativeAgents: Guid list
    }

    let selectAgent (agents: Agent list) (criteria: SelectionCriteria) (performances: Map<Guid, AgentPerformanceTracker.PerformanceMetrics>) : SelectionResult =
        let matchingAgents = 
            agents 
            |> List.filter (fun a -> a.Type = criteria.AgentType && a.IsActive)
        
        if matchingAgents.IsEmpty then
            {
                SelectedAgent = None
                Reason = "No active agents of the specified type found"
                AlternativeAgents = []
            }
        else
            let scoredAgents = 
                matchingAgents
                |> List.map (fun agent ->
                    let perf = performances.TryFind agent.Id |> Option.defaultValue { TotalExecutions = 0; SuccessCount = 0; FailureCount = 0; AverageExecutionTime = 0.0; SuccessRate = 0.0; LastExecutionTime = None }
                    let capabilityMatch = AgentCapabilityMatcher.matchCapabilities agent criteria.RequiredCapabilities
                    
                    let score = 
                        capabilityMatch.MatchScore * 0.5 +
                        (if perf.SuccessRate >= criteria.MinSuccessRate then 1.0 else 0.0) * 0.3 +
                        (if perf.AverageExecutionTime <= criteria.MaxExecutionTime then 1.0 else 0.0) * 0.2
                    
                    (agent, score, capabilityMatch)
                )
            
            let bestAgent = scoredAgents |> List.maxBy (fun (_, score, _) -> score)
            
            let alternatives = 
                scoredAgents
                |> List.filter (fun (a, s, _) -> a.Id <> (bestAgent |> fun (agent, _, _) -> agent).Id && s > 0.5)
                |> List.map (fun (a, _, _) -> a.Id)
            
            {
                SelectedAgent = Some (bestAgent |> fun (agent, _, _) -> agent).Id
                Reason = sprintf "Agent selected with score %.2f" (bestAgent |> fun (_, score, _) -> score)
                AlternativeAgents = alternatives
            }

    // Select agent using AI for intelligent matching
    let selectAgentWithAI (aiService: IAIService) (agents: Agent list) (criteria: SelectionCriteria) (performances: Map<Guid, AgentPerformanceTracker.PerformanceMetrics>) (taskDescription: string) : Async<SelectionResult> =
        async {
            let agentDescriptions = 
                agents 
                |> List.map (fun a -> sprintf "Agent %s: type %s, tools [%s]" (a.Id.ToString()) (string a.Type) (a.AllowedTools |> Seq.map (fun t -> t.Name) |> String.concat ", "))
                |> String.concat "; "
            
            let criteriaText = sprintf "type %s, capabilities [%s], min success %.2f, max time %.1fms" (string criteria.AgentType) (String.concat ", " criteria.RequiredCapabilities) criteria.MinSuccessRate criteria.MaxExecutionTime
            
            let prompt = sprintf "Select best agent for task '%s'. Available: [%s]. Requirements: [%s]. Return JSON: {\"selectedAgentId\": string (guid), \"reason\": string, \"alternatives\": [string (guid)]}" taskDescription agentDescriptions criteriaText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "ai") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let selectedAgentId = 
                try
                    let idStr = root.GetProperty("selectedAgentId").GetString()
                    Some (Guid.Parse(idStr))
                with _ -> None
            
            let reason = 
                try root.GetProperty("reason").GetString()
                with _ -> "No suitable agent found"
            
            let alternatives = 
                try
                    root.GetProperty("alternatives").EnumerateArray()
                    |> Seq.map (fun a -> Guid.Parse(a.GetString()))
                    |> List.ofSeq
                with _ -> []
            
            return {
                SelectedAgent = selectedAgentId
                Reason = reason
                AlternativeAgents = alternatives
            }
        }

// Agent Tool Validator
module AgentToolValidator =

    type ValidationResult = {
        IsValid: bool
        Errors: string list
        Warnings: string list
    }

    let validateTools (agent: Agent) : ValidationResult =
        let errors = ResizeArray<string>()
        let warnings = ResizeArray<string>()
        
        if agent.AllowedTools |> Seq.isEmpty then
            warnings.Add("Agent has no tools configured")
        
        let toolNames = agent.AllowedTools |> Seq.map (fun t -> t.Name) |> Seq.toList
        let duplicates = toolNames |> List.groupBy id |> List.filter (fun (_, items) -> items.Length > 1) |> List.map fst
        if not (List.isEmpty duplicates) then
            errors.AddRange(duplicates |> List.map (fun name -> sprintf "Duplicate tool: %s" name))
        
        for tool in agent.AllowedTools do
            if String.IsNullOrWhiteSpace(tool.Parameters) then
                warnings.Add(sprintf "Tool '%s' has no parameters defined" tool.Name)
        
        {
            IsValid = errors.Count = 0
            Errors = List.ofSeq errors
            Warnings = List.ofSeq warnings
        }

    // Validate tools using AI for semantic validation
    let validateToolsWithAI (aiService: IAIService) (agent: Agent) (expectedCapabilities: string list) : Async<ValidationResult> =
        async {
            let toolDescriptions = agent.AllowedTools |> Seq.map (fun t -> sprintf "%s: %s" t.Name t.Parameters) |> String.concat "; "
            let expectedText = String.concat ", " expectedCapabilities
            let prompt = sprintf "Validate agent tools: configured [%s], expected [%s]. Return JSON: {\"isValid\": bool, \"errors\": [string], \"warnings\": [string]}" toolDescriptions expectedText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "ai") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let isValid = 
                try root.GetProperty("isValid").GetBoolean()
                with _ -> true
            
            let errors = 
                try
                    root.GetProperty("errors").EnumerateArray()
                    |> Seq.map (fun e -> e.GetString())
                    |> List.ofSeq
                with _ -> []
            
            let warnings = 
                try
                    root.GetProperty("warnings").EnumerateArray()
                    |> Seq.map (fun w -> w.GetString())
                    |> List.ofSeq
                with _ -> []
            
            return {
                IsValid = isValid
                Errors = errors
                Warnings = warnings
            }
        }
