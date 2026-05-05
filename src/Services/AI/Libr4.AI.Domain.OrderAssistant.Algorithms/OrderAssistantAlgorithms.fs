namespace Libr4.AI.Domain.OrderAssistant.Algorithms

open System
open System.Text.Json
open Libr4.AI.Domain.OrderAssistant
open Libr4.AI.Infrastructure.AI

// Budget Estimator
module BudgetEstimator =

    type BudgetEstimate = {
        MinBudget: int
        MaxBudget: int
        RecommendedBudget: int
        Confidence: float32
    }

    // Estimate budget based on task complexity and market rates using AI
    let estimateBudget (aiService: IAIService) (complexityScore: int) (estimatedHours: int) (marketRate: int) : Async<BudgetEstimate> =
        async {
            let prompt = sprintf "Estimate budget for task: complexity score %d/10, estimated hours %d, market rate $%d/hour. Return JSON: {\"minBudget\": number, \"maxBudget\": number, \"recommendedBudget\": number, \"confidence\": number (0-1)}" complexityScore estimatedHours marketRate
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "budget") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let minBudget = 
                try root.GetProperty("minBudget").GetInt32()
                with _ ->
                    let baseBudget = estimatedHours * marketRate
                    int (float32 baseBudget * 0.8f)
            
            let maxBudget = 
                try root.GetProperty("maxBudget").GetInt32()
                with _ ->
                    let baseBudget = estimatedHours * marketRate
                    int (float32 baseBudget * 1.5f)
            
            let recommendedBudget = 
                try root.GetProperty("recommendedBudget").GetInt32()
                with _ ->
                    let baseBudget = estimatedHours * marketRate
                    let complexityMultiplier = 1.0f + (float32 complexityScore / 10f)
                    int (float32 baseBudget * complexityMultiplier)
            
            let confidence = 
                try root.GetProperty("confidence").GetSingle()
                with _ ->
                    if complexityScore > 7 then 0.6f
                    elif complexityScore > 4 then 0.8f
                    else 0.9f
            
            return {
                MinBudget = minBudget
                MaxBudget = maxBudget
                RecommendedBudget = recommendedBudget
                Confidence = confidence
            }
        }

// Duration Predictor
module DurationPredictor =

    type DurationPrediction = {
        MinDays: int
        MaxDays: int
        EstimatedDays: int
        Milestones: string list
    }

    // Predict task duration based on complexity and scope using AI
    let predictDuration (aiService: IAIService) (complexityScore: int) (wordCount: int) : Async<DurationPrediction> =
        async {
            let prompt = sprintf "Predict task duration: complexity score %d/10, word count %d. Return JSON: {\"minDays\": number, \"maxDays\": number, \"estimatedDays\": number, \"milestones\": [string, string, string, string]}" complexityScore wordCount
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "duration") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let minDays = 
                try root.GetProperty("minDays").GetInt32()
                with _ ->
                    let baseDays = wordCount / 100
                    let complexityMultiplier = 1.0f + (float32 complexityScore / 10f)
                    int (float32 baseDays * complexityMultiplier * 0.7f) |> max 1
            
            let maxDays = 
                try root.GetProperty("maxDays").GetInt32()
                with _ ->
                    let baseDays = wordCount / 100
                    let complexityMultiplier = 1.0f + (float32 complexityScore / 10f)
                    int (float32 baseDays * complexityMultiplier * 1.5f)
            
            let estimatedDays = 
                try root.GetProperty("estimatedDays").GetInt32()
                with _ ->
                    let baseDays = wordCount / 100
                    let complexityMultiplier = 1.0f + (float32 complexityScore / 10f)
                    int (float32 baseDays * complexityMultiplier) |> max 1
            
            let milestones = 
                try
                    root.GetProperty("milestones").EnumerateArray()
                    |> Seq.map (fun m -> m.GetString())
                    |> List.ofSeq
                with _ ->
                    [
                        sprintf "Day %d: Initial analysis and planning" (estimatedDays / 5)
                        sprintf "Day %d: First draft/implementation" (estimatedDays / 3)
                        sprintf "Day %d: Review and refinement" (estimatedDays / 2)
                        sprintf "Day %d: Final delivery" estimatedDays
                    ]
            
            return {
                MinDays = minDays
                MaxDays = maxDays
                EstimatedDays = estimatedDays
                Milestones = milestones
            }
        }

// Freelancer Matcher
module FreelancerMatcher =

    type FreelancerMatch = {
        FreelancerId: Guid
        Name: string
        MatchScore: float32
        Rate: int
        Availability: string
    }

    // Match freelancers based on task requirements using AI
    let matchFreelancers (aiService: IAIService) (taskSkills: string list) (taskBudget: int) (freelancers: (Guid * string * string list * int * string) list) : Async<FreelancerMatch list> =
        async {
            let taskSkillsText = taskSkills |> String.concat ", "
            let freelancersText = freelancers |> List.map (fun (_, name, skills, rate, avail) -> sprintf "%s: skills [%s], rate $%d, availability %s" name (skills |> String.concat ", ") rate avail) |> String.concat "; "
            
            let prompt = sprintf "Match freelancers for task: required skills [%s], budget $%d, available freelancers: [%s]. Return JSON: {\"matchedFreelancers\": [{\"name\": string, \"matchScore\": number (0-100)}]}" taskSkillsText taskBudget freelancersText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "matching") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let aiMatchedNames = 
                try
                    root.GetProperty("matchedFreelancers").EnumerateArray()
                    |> Seq.map (fun f -> (f.GetProperty("name").GetString(), f.GetProperty("matchScore").GetSingle()))
                    |> Map.ofSeq
                with _ -> Map.empty
            
            let matches = 
                freelancers
                |> List.map (fun (id, name, skills, rate, availability) ->
                    let skillMatches = 
                        taskSkills
                        |> List.filter (fun ts -> skills |> List.exists (fun s -> s.ToLower().Contains(ts.ToLower())))
                        |> List.length
                    
                    let skillScore = 
                        if taskSkills.IsEmpty then 100f
                        else float32 skillMatches / float32 taskSkills.Length * 100f
                    
                    let budgetScore = 
                        if taskBudget >= rate * 10 then 100f
                        elif taskBudget >= rate * 5 then 80f
                        elif taskBudget >= rate then 60f
                        else 0f
                    
                    let heuristicScore = skillScore * 0.7f + budgetScore * 0.3f
                    
                    let matchScore = 
                        match aiMatchedNames.TryFind name with
                        | Some aiScore -> (aiScore + heuristicScore) / 2f
                        | None -> heuristicScore
                    
                    {
                        FreelancerId = id
                        Name = name
                        MatchScore = matchScore
                        Rate = rate
                        Availability = availability
                    })
                |> List.filter (fun f -> f.MatchScore > 40f)
                |> List.sortByDescending (fun f -> f.MatchScore)
                |> List.take 5
            
            return matches
        }
