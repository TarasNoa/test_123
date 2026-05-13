namespace Libr4.Tasks.Domain.TaskAnalytics.Algorithms

open System
open System.Text.Json
open Libr4.Tasks.Domain.TaskAnalytics
open Libr4.AI.Application.Abstractions

// Metrics Calculator
module MetricsCalculator =

    type TaskMetrics = {
        Views: int
        Applications: int
        UniqueVisitors: int
        ConversionRate: float32
        EngagementScore: float32
    }

    // Calculate task engagement metrics using AI
    let calculateMetrics (aiService: IAIService) (views: int) (applications: int) (uniqueVisitors: int) : Async<TaskMetrics> =
        async {
            let conversionRate = 
                if views > 0 then float32 applications / float32 views * 100f
                else 0f
            
            let prompt = sprintf "Calculate engagement score for task with %d views, %d applications, %d unique visitors. Return JSON: {\"engagementScore\": number (0-100)}" views applications uniqueVisitors
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "analytics") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let engagementScore = 
                try root.GetProperty("engagementScore").GetSingle()
                with _ ->
                    let viewScore = float32 views * 0.3f |> min 30f
                    let applicationScore = float32 applications * 5f |> min 40f
                    let visitorScore = float32 uniqueVisitors * 2f |> min 30f
                    viewScore + applicationScore + visitorScore |> min 100f
            
            return {
                Views = views
                Applications = applications
                UniqueVisitors = uniqueVisitors
                ConversionRate = conversionRate
                EngagementScore = engagementScore
            }
        }

// Performance Tracker
module PerformanceTracker =

    type PerformanceMetrics = {
        AverageRating: float32
        CompletionRate: float32
        AverageCompletionTime: int // days
        DisputeRate: float32
        OverallScore: float32
    }

    // Track task performance metrics using AI
    let trackPerformance (aiService: IAIService) (ratings: float32 list) (completions: int) (totalTasks: int) (completionTimes: int list) (disputes: int) : Async<PerformanceMetrics> =
        async {
            let averageRating = 
                if ratings.IsEmpty then 0f
                else ratings |> List.average
            
            let completionRate = 
                if totalTasks > 0 then float32 completions / float32 totalTasks * 100f
                else 0f
            
            let averageCompletionTime = 
                if completionTimes.IsEmpty then 0
                else completionTimes |> List.map float32 |> List.average |> int
            
            let disputeRate = 
                if totalTasks > 0 then float32 disputes / float32 totalTasks * 100f
                else 0f
            
            let prompt = sprintf "Calculate overall performance score: avg rating %.1f, completion rate %.1f%%, avg completion time %d days, dispute rate %.1f%%. Return JSON: {\"overallScore\": number (0-100)}" averageRating completionRate averageCompletionTime disputeRate
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "performance") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let overallScore = 
                try root.GetProperty("overallScore").GetSingle()
                with _ ->
                    let ratingScore = averageRating * 20f
                    let completionScore = completionRate * 0.4f
                    let timeScore = 
                        if averageCompletionTime <= 7 then 20f
                        elif averageCompletionTime <= 14 then 15f
                        elif averageCompletionTime <= 30 then 10f
                        else 5f
                    let disputePenalty = disputeRate * -0.5f
                    ratingScore + completionScore + timeScore + disputePenalty |> max 0f |> min 100f
            
            return {
                AverageRating = averageRating
                CompletionRate = completionRate
                AverageCompletionTime = averageCompletionTime
                DisputeRate = disputeRate
                OverallScore = overallScore
            }
        }

// Trend Analyzer
module TrendAnalyzer =

    type Trend = {
        Direction: string // Increasing, Decreasing, Stable
        ChangePercentage: float32
        Recommendation: string
    }

    // Analyze trends in task analytics using AI
    let analyzeTrend (aiService: IAIService) (currentValue: int) (previousValue: int) (metricName: string) : Async<Trend> =
        async {
            if previousValue = 0 then
                return {
                    Direction = "Stable"
                    ChangePercentage = 0f
                    Recommendation = "Insufficient data for trend analysis"
                }
            else
                let change = float32 (currentValue - previousValue) / float32 previousValue * 100f
                let direction = 
                    if change > 5f then "Increasing"
                    elif change < -5f then "Decreasing"
                    else "Stable"
                
                let prompt = sprintf "Analyze trend for metric '%s': current %d, previous %d, change %.1f%%. Return JSON: {\"recommendation\": string}" metricName currentValue previousValue change
                
                let! aiResponse = aiService.AnalyzeTextAsync(prompt, "trend") |> Async.AwaitTask
                
                let jsonDoc = JsonDocument.Parse(aiResponse)
                let root = jsonDoc.RootElement
                
                let recommendation = 
                    try root.GetProperty("recommendation").GetString()
                    with _ ->
                        match metricName, direction with
                        | "Views", "Decreasing" -> "Consider improving task visibility and marketing"
                        | "Applications", "Decreasing" -> "Review task requirements and compensation"
                        | "CompletionRate", "Decreasing" -> "Investigate completion barriers"
                        | _, "Increasing" -> "Maintain current strategy"
                        | _ -> "Monitor trends and adjust as needed"
                
                return {
                    Direction = direction
                    ChangePercentage = change
                    Recommendation = recommendation
                }
        }
