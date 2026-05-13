namespace Libr4.AI.Domain.TaskAnalysis.Algorithms

open System
open System.Text.Json
open Libr4.AI.Domain.TaskAnalysis
open Libr4.AI.Application.Abstractions

// Complexity Calculator
module ComplexityCalculator =

    type ComplexityResult = {
        Score: int
        Level: string
        EstimatedHours: int
        EstimatedCost: int
    }

    let calculateComplexity (aiService: IAIService) (description: string) (requiredSkills: string list) (budget: int option) : Async<ComplexityResult> =
        async {
            let systemPrompt = "You are a task complexity analyst. Analyze the task description and return complexity in JSON format: {\"score\": number (1-10), \"level\": string (Low/Medium/High), \"estimatedHours\": number, \"estimatedCost\": number}"
            let skillsText = String.concat ", " requiredSkills
            let budgetText = match budget with Some b -> sprintf "$%d" b | None -> "not specified"
            let prompt = sprintf "Analyze complexity for task: %s. Required skills: %s. Budget: %s" description skillsText budgetText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "complexity") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let score = root.GetProperty("score").GetInt32()
            let level = root.GetProperty("level").GetString()
            let estimatedHours = root.GetProperty("estimatedHours").GetInt32()
            let estimatedCost = root.GetProperty("estimatedCost").GetInt32()
            
            return {
                Score = score
                Level = level
                EstimatedHours = estimatedHours
                EstimatedCost = estimatedCost
            }
        }

// Skill Extractor
module SkillExtractor =

    type SkillMatch = {
        Skill: string
        Confidence: float32
        Category: string
    }

    let extractSkills (aiService: IAIService) (description: string) (allSkills: (string * string) list) : Async<SkillMatch list> =
        async {
            let systemPrompt = "You are a skills extraction expert. Extract all technical and soft skills from the task description and return as a JSON array: [{\"skill\": string, \"confidence\": number (0-100), \"category\": string}]"
            let prompt = sprintf "Extract skills from this task description: %s" description
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "skills") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let matches = 
                jsonDoc.RootElement.EnumerateArray()
                |> Seq.map (fun element ->
                    {
                        Skill = element.GetProperty("skill").GetString()
                        Confidence = element.GetProperty("confidence").GetSingle()
                        Category = element.GetProperty("category").GetString()
                    })
                |> List.ofSeq
            
            return matches
        }

// Risk Assessor
module RiskAssessor =

    type RiskFactor = {
        Factor: string
        Severity: string
        Mitigation: string
    }

    let assessRisks (aiService: IAIService) (description: string) (budget: int option) (deadline: DateTimeOffset option) : Async<RiskFactor list> =
        async {
            let systemPrompt = "You are a risk assessment expert. Analyze the project/task and return risks in JSON array format: [{\"factor\": string, \"severity\": string (Low/Medium/High), \"mitigation\": string}]"
            let budgetText = match budget with Some b -> sprintf "$%d" b | None -> "not specified"
            let deadlineText = match deadline with Some d -> d.ToString("yyyy-MM-dd") | None -> "not specified"
            let prompt = sprintf "Assess risks for task with budget %s and deadline %s. Description: %s" budgetText deadlineText description
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "risk") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let risks = 
                jsonDoc.RootElement.EnumerateArray()
                |> Seq.map (fun element ->
                    {
                        Factor = element.GetProperty("factor").GetString()
                        Severity = element.GetProperty("severity").GetString()
                        Mitigation = element.GetProperty("mitigation").GetString()
                    })
                |> List.ofSeq
            
            return risks
        }
