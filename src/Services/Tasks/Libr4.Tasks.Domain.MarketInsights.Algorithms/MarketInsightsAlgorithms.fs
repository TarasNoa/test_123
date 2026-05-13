namespace Libr4.Tasks.Domain.MarketInsights.Algorithms

open System
open System.Text.Json
open Libr4.Tasks.Domain.MarketInsights
open Libr4.AI.Application.Abstractions

// Pricing Analyzer
module PricingAnalyzer =

    type PricingInsight = {
        AverageRate: int
        MinRate: int
        MaxRate: int
        RateTrend: string // Increasing, Decreasing, Stable
        RecommendedRate: int
    }

    // Analyze market pricing trends using AI
    let analyzePricing (aiService: IAIService) (rates: int list) (category: string) : Async<PricingInsight> =
        async {
            if rates.IsEmpty then
                return {
                    AverageRate = 0
                    MinRate = 0
                    MaxRate = 0
                    RateTrend = "No data"
                    RecommendedRate = 0
                }
            else
                let averageRate = rates |> List.map float32 |> List.average
                let minRate = rates |> List.min
                let maxRate = rates |> List.max
                
                let ratesText = rates |> List.map string |> String.concat ", "
                let prompt = sprintf "Analyze pricing trend for category '%s' with rates: [%s]. Return JSON: {\"rateTrend\": \"Increasing/Decreasing/Stable\", \"recommendedRate\": number}" category ratesText
                
                let! aiResponse = aiService.AnalyzeTextAsync(prompt, "pricing") |> Async.AwaitTask
                
                let jsonDoc = JsonDocument.Parse(aiResponse)
                let root = jsonDoc.RootElement
                
                let rateTrend = 
                    try root.GetProperty("rateTrend").GetString()
                    with _ -> "Stable"
                
                let recommendedRate = 
                    try root.GetProperty("recommendedRate").GetInt32()
                    with _ -> int (averageRate + (float32 maxRate - averageRate) / 2f)
                
                return {
                    AverageRate = int averageRate
                    MinRate = minRate
                    MaxRate = maxRate
                    RateTrend = rateTrend
                    RecommendedRate = recommendedRate
                }
        }

// Demand Forecaster
module DemandForecaster =

    type DemandForecast = {
        CurrentDemand: string // Low, Medium, High
        ProjectedDemand: string
        Trend: string // Growing, Stable, Declining
        Recommendation: string
    }

    // Forecast demand for skills/categories using AI
    let forecastDemand (aiService: IAIService) (historicalData: (string * int) list) (category: string) : Async<DemandForecast> =
        async {
            if historicalData.IsEmpty then
                return {
                    CurrentDemand = "Unknown"
                    ProjectedDemand = "Unknown"
                    Trend = "No data"
                    Recommendation = "Insufficient data for forecast"
                }
            else
                let recentDemand = historicalData |> List.map snd |> List.map float32 |> List.average |> int
                let currentDemand = 
                    if recentDemand < 50 then "Low"
                    elif recentDemand < 100 then "Medium"
                    else "High"
                
                let dataText = historicalData |> List.map (fun (d, v) -> sprintf "%s:%d" d v) |> String.concat ", "
                let prompt = sprintf "Forecast demand for category '%s' with historical data [%s]. Return JSON: {\"trend\": \"Growing/Stable/Declining\", \"projectedDemand\": \"Low/Medium/High\", \"recommendation\": string}" category dataText
                
                let! aiResponse = aiService.AnalyzeTextAsync(prompt, "demand") |> Async.AwaitTask
                
                let jsonDoc = JsonDocument.Parse(aiResponse)
                let root = jsonDoc.RootElement
                
                let trend = 
                    try root.GetProperty("trend").GetString()
                    with _ -> "Stable"
                
                let projectedDemand = 
                    try root.GetProperty("projectedDemand").GetString()
                    with _ -> currentDemand
                
                let recommendation = 
                    try root.GetProperty("recommendation").GetString()
                    with _ -> "Monitor market trends for opportunities"
                
                return {
                    CurrentDemand = currentDemand
                    ProjectedDemand = projectedDemand
                    Trend = trend
                    Recommendation = recommendation
                }
        }

// Skill Demand Tracker
module SkillDemandTracker =

    type SkillDemand = {
        Skill: string
        DemandLevel: string // Low, Medium, High
        GrowthRate: float32
        TopCategories: string list
        ActionItems: string list
    }

    // Track demand for specific skills using AI
    let trackSkillDemand (aiService: IAIService) (skill: string) (categoryDemand: (string * int) list) (historicalGrowth: float32 list) : Async<SkillDemand> =
        async {
            let relevantCategories = categoryDemand |> List.filter (fun (cat, _) -> cat.ToLower().Contains(skill.ToLower()))
            
            let demandLevel = 
                if relevantCategories.IsEmpty then "Low"
                else
                    let avgDemand = relevantCategories |> List.map snd |> List.map float32 |> List.average
                    if avgDemand < 50f then "Low"
                    elif avgDemand < 100f then "Medium"
                    else "High"
            
            let growthRate = 
                if historicalGrowth.IsEmpty then 0f
                else historicalGrowth |> List.average
            
            let topCategories = 
                relevantCategories
                |> List.sortByDescending snd
                |> List.take 3
                |> List.map fst
            
            let categoriesText = topCategories |> String.concat ", "
            let prompt = sprintf "Analyze demand for skill '%s' in categories [%s] with growth rate %.1f. Return JSON: {\"actionItems\": [string, string, string]}" skill categoriesText growthRate
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "skills") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let actionItems = 
                try
                    root.GetProperty("actionItems").EnumerateArray()
                    |> Seq.map (fun a -> a.GetString())
                    |> List.ofSeq
                with _ ->
                    match demandLevel, growthRate with
                    | "High", g when g > 0.1f -> ["High demand with strong growth - prioritize this skill"]
                    | "High", _ -> ["High demand - maintain and improve this skill"]
                    | "Medium", g when g > 0.1f -> ["Growing demand - consider developing this skill further"]
                    | "Medium", _ -> ["Moderate demand - keep this skill updated"]
                    | "Low", g when g > 0.1f -> ["Emerging skill - early opportunity to develop"]
                    | "Low", _ -> ["Low demand - may not be a priority"]
                    | _, _ -> ["Monitor market trends"]
            
            return {
                Skill = skill
                DemandLevel = demandLevel
                GrowthRate = growthRate
                TopCategories = topCategories
                ActionItems = actionItems
            }
        }
