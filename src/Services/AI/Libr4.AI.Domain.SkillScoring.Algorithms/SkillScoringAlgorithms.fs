namespace Libr4.AI.Domain.SkillScoring.Algorithms

open System
open System.Text.Json
open Libr4.AI.Domain.SkillScoring
open Libr4.AI.Application.Abstractions

// Proficiency Calculator
module ProficiencyCalculator =

    type ProficiencyResult = {
        Score: float32
        Level: string
        Trend: string // Improving, Stable, Declining
    }

    // Calculate skill proficiency using AI service
    let calculateProficiency (aiService: IAIService) (usageCount: int) (successRate: float32) (lastUsedDays: int) : Async<ProficiencyResult> =
        async {
            let systemPrompt = "You are a skill proficiency analyst. Analyze skill usage data and return proficiency score (0-100), level (Beginner/Intermediate/Advanced/Expert), and trend (Improving/Stable/Declining) in JSON format: {\"score\": number, \"level\": string, \"trend\": string}"
            let prompt = sprintf "Analyze skill proficiency: usage count %d, success rate %.1f%%, last used %d days ago" usageCount successRate lastUsedDays
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "proficiency") |> Async.AwaitTask
            
            // Parse AI JSON response
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let score = root.GetProperty("score").GetSingle()
            let level = root.GetProperty("level").GetString()
            let trend = root.GetProperty("trend").GetString()
            
            return {
                Score = score
                Level = level
                Trend = trend
            }
        }

// Skill Gap Analyzer
module SkillGapAnalyzer =

    type SkillGap = {
        Skill: string
        CurrentLevel: string
        RequiredLevel: string
        Gap: string // None, Small, Medium, Large
        Priority: string // Low, Medium, High
    }

    // Analyze skill gaps using AI service
    let analyzeSkillGaps (aiService: IAIService) (currentSkills: (string * float32) list) (requiredSkills: (string * float32) list) : Async<SkillGap list> =
        async {
            let systemPrompt = "You are a skill gap analyst. Compare current skills with required skills and return skill gaps in JSON array format: [{\"skill\": string, \"currentLevel\": string, \"requiredLevel\": string, \"gap\": string, \"priority\": string}]"
            let currentText = currentSkills |> List.map (fun (s, score) -> sprintf "%s (%.1f%%)" s score) |> String.concat ", "
            let requiredText = requiredSkills |> List.map (fun (s, score) -> sprintf "%s (%.1f%%)" s score) |> String.concat ", "
            let prompt = sprintf "Analyze skill gaps. Current skills: [%s]. Required skills: [%s]" currentText requiredText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "skills") |> Async.AwaitTask
            
            // Parse AI JSON response
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let gaps = 
                jsonDoc.RootElement.EnumerateArray()
                |> Seq.map (fun element ->
                    {
                        Skill = element.GetProperty("skill").GetString()
                        CurrentLevel = element.GetProperty("currentLevel").GetString()
                        RequiredLevel = element.GetProperty("requiredLevel").GetString()
                        Gap = element.GetProperty("gap").GetString()
                        Priority = element.GetProperty("priority").GetString()
                    })
                |> List.ofSeq
            
            return gaps
        }

// Improvement Recommender
module ImprovementRecommender =

    type ImprovementRecommendation = {
        Skill: string
        Action: string
        EstimatedTime: string
        Resources: string list
    }

    // Recommend improvements using AI service
    let recommendImprovements (aiService: IAIService) (skillGaps: SkillGapAnalyzer.SkillGap list) : Async<ImprovementRecommendation list> =
        async {
            let systemPrompt = "You are a learning path recommender. Analyze skill gaps and provide personalized improvement recommendations in JSON array format: [{\"skill\": string, \"action\": string, \"estimatedTime\": string, \"resources\": string[]}]"
            let gapsText = skillGaps |> List.map (fun g -> sprintf "%s: %s -> %s (gap: %s, priority: %s)" g.Skill g.CurrentLevel g.RequiredLevel g.Gap g.Priority) |> String.concat "; "
            let prompt = sprintf "Recommend improvements for skill gaps: %s" gapsText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "skills") |> Async.AwaitTask
            
            // Parse AI JSON response
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let recommendations = 
                jsonDoc.RootElement.EnumerateArray()
                |> Seq.map (fun element ->
                    let resources = 
                        element.GetProperty("resources").EnumerateArray()
                        |> Seq.map (fun r -> r.GetString())
                        |> List.ofSeq
                    
                    {
                        Skill = element.GetProperty("skill").GetString()
                        Action = element.GetProperty("action").GetString()
                        EstimatedTime = element.GetProperty("estimatedTime").GetString()
                        Resources = resources
                    })
                |> List.ofSeq
            
            return recommendations
        }
