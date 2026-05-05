namespace Libr4.Social.Domain.CommunityStats.Algorithms

open System
open System.Text.Json
open Libr4.AI.Infrastructure.AI
open Libr4.Social.Domain.CommunityStats

// Engagement Calculator
module EngagementCalculator =

    type EngagementMetrics = {
        EngagementRate: float32
        ActiveUserRate: float32
        InteractionRate: float32
        RetentionRate: float32
    }

    // Calculate community engagement metrics
    let calculateEngagement (totalMembers: int) (activeMembers: int) (totalPosts: int) (totalInteractions: int) (newMembers: int) : EngagementMetrics =
        if totalMembers = 0 then
            {
                EngagementRate = 0f
                ActiveUserRate = 0f
                InteractionRate = 0f
                RetentionRate = 0f
            }
        else
            let engagementRate = float32 totalInteractions / float32 totalMembers * 100f |> min 100f
            let activeUserRate = float32 activeMembers / float32 totalMembers * 100f |> min 100f
            let interactionRate = if totalPosts > 0 then float32 totalInteractions / float32 totalPosts else 0f |> min 100f
            let retentionRate = if newMembers > 0 then float32 activeMembers / float32 newMembers * 100f |> min 100f else 0f
            
            {
                EngagementRate = engagementRate
                ActiveUserRate = activeUserRate
                InteractionRate = interactionRate
                RetentionRate = retentionRate
            }

    // Calculate engagement using AI for intelligent metrics analysis
    let calculateEngagementWithAI (aiService: IAIService) (totalMembers: int) (activeMembers: int) (totalPosts: int) (totalInteractions: int) (newMembers: int) (engagementContext: string) : Async<EngagementMetrics> =
        async {
            let prompt = sprintf "Calculate engagement: total %d, active %d, posts %d, interactions %d, new %d, context '%s'. Return JSON: {\"engagementRate\": number, \"activeUserRate\": number, \"interactionRate\": number, \"retentionRate\": number}" totalMembers activeMembers totalPosts totalInteractions newMembers engagementContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "social") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcFallback() = calculateEngagement totalMembers activeMembers totalPosts totalInteractions newMembers
            
            let engagementRate = try root.GetProperty("engagementRate").GetSingle() with _ -> calcFallback().EngagementRate
            let activeUserRate = try root.GetProperty("activeUserRate").GetSingle() with _ -> calcFallback().ActiveUserRate
            let interactionRate = try root.GetProperty("interactionRate").GetSingle() with _ -> calcFallback().InteractionRate
            let retentionRate = try root.GetProperty("retentionRate").GetSingle() with _ -> calcFallback().RetentionRate
            
            return {
                EngagementRate = engagementRate
                ActiveUserRate = activeUserRate
                InteractionRate = interactionRate
                RetentionRate = retentionRate
            }
        }

// Growth Tracker
module GrowthTracker =

    type GrowthMetrics = {
        MemberGrowth: float32
        ContentGrowth: float32
        InteractionGrowth: float32
        Velocity: string // slow, moderate, fast
    }

    // Track community growth over time
    let trackGrowth (previousMembers: int) (currentMembers: int) (previousPosts: int) (currentPosts: int) (previousInteractions: int) (currentInteractions: int) : GrowthMetrics =
        let memberGrowth = 
            if previousMembers = 0 then 0f
            else (float32 (currentMembers - previousMembers) / float32 previousMembers) * 100f
        
        let contentGrowth = 
            if previousPosts = 0 then 0f
            else (float32 (currentPosts - previousPosts) / float32 previousPosts) * 100f
        
        let interactionGrowth = 
            if previousInteractions = 0 then 0f
            else (float32 (currentInteractions - previousInteractions) / float32 previousInteractions) * 100f
        
        let avgGrowth = (memberGrowth + contentGrowth + interactionGrowth) / 3f
        let velocity = 
            if avgGrowth > 20f then "fast"
            elif avgGrowth > 5f then "moderate"
            else "slow"
        
        {
            MemberGrowth = memberGrowth
            ContentGrowth = contentGrowth
            InteractionGrowth = interactionGrowth
            Velocity = velocity
        }

    // Track growth using AI for intelligent growth analysis
    let trackGrowthWithAI (aiService: IAIService) (previousMembers: int) (currentMembers: int) (previousPosts: int) (currentPosts: int) (previousInteractions: int) (currentInteractions: int) (growthContext: string) : Async<GrowthMetrics> =
        async {
            let prompt = sprintf "Track growth: members %d->%d, posts %d->%d, interactions %d->%d, context '%s'. Return JSON: {\"memberGrowth\": number, \"contentGrowth\": number, \"interactionGrowth\": number, \"velocity\": string}" previousMembers currentMembers previousPosts currentPosts previousInteractions currentInteractions growthContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "social") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcFallback() = trackGrowth previousMembers currentMembers previousPosts currentPosts previousInteractions currentInteractions
            
            let memberGrowth = try root.GetProperty("memberGrowth").GetSingle() with _ -> calcFallback().MemberGrowth
            let contentGrowth = try root.GetProperty("contentGrowth").GetSingle() with _ -> calcFallback().ContentGrowth
            let interactionGrowth = try root.GetProperty("interactionGrowth").GetSingle() with _ -> calcFallback().InteractionGrowth
            let velocity = try root.GetProperty("velocity").GetString() with _ -> calcFallback().Velocity
            
            return {
                MemberGrowth = memberGrowth
                ContentGrowth = contentGrowth
                InteractionGrowth = interactionGrowth
                Velocity = velocity
            }
        }

// Activity Scorer
module ActivityScorer =

    type ActivityScore = {
        Score: float32
        Level: string // inactive, low, medium, high, very_high
        Trend: string // declining, stable, increasing
    }

    // Calculate user activity score
    let calculateActivityScore (postsCount: int) (interactionsCount: int) (connectionsCount: int) (previousScore: float32) : ActivityScore =
        let rawScore = (float32 postsCount * 2f + float32 interactionsCount * 1f + float32 connectionsCount * 0.5f) / 3f
        
        let score = min 100f rawScore
        let level = 
            if score < 10f then "inactive"
            elif score < 30f then "low"
            elif score < 50f then "medium"
            elif score < 75f then "high"
            else "very_high"
        
        let trend = 
            if score < previousScore - 5f then "declining"
            elif score > previousScore + 5f then "increasing"
            else "stable"
        
        {
            Score = score
            Level = level
            Trend = trend
        }

    // Calculate activity score using AI for intelligent activity analysis
    let calculateActivityScoreWithAI (aiService: IAIService) (postsCount: int) (interactionsCount: int) (connectionsCount: int) (previousScore: float32) (activityContext: string) : Async<ActivityScore> =
        async {
            let prompt = sprintf "Calculate activity: posts %d, interactions %d, connections %d, previous %.1f, context '%s'. Return JSON: {\"score\": number, \"level\": string, \"trend\": string}" postsCount interactionsCount connectionsCount previousScore activityContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "social") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcFallback() = calculateActivityScore postsCount interactionsCount connectionsCount previousScore
            
            let score = try root.GetProperty("score").GetSingle() with _ -> calcFallback().Score
            let level = try root.GetProperty("level").GetString() with _ -> calcFallback().Level
            let trend = try root.GetProperty("trend").GetString() with _ -> calcFallback().Trend
            
            return {
                Score = score
                Level = level
                Trend = trend
            }
        }
