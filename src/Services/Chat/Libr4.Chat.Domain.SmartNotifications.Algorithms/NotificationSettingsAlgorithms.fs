namespace Libr4.Chat.Domain.SmartNotifications.Algorithms

open System
open System.Text.Json
open Libr4.AI.Application.Abstractions

// Preference Matcher
module PreferenceMatcher =

    type NotificationType = {
        Category: string
        Priority: string
    }

    type UserPreference = {
        NotificationType: string
        Enabled: bool
        PreferredChannel: string
    }

    type MatchResult = {
        ShouldSend: bool
        Channel: string
        Reason: string
    }

    // Match notification type with user preferences
    let matchPreference (notificationType: NotificationType) (preferences: UserPreference list) : MatchResult =
        let exactMatch = 
            preferences 
            |> List.tryFind (fun p -> p.NotificationType = notificationType.Category)
        
        match exactMatch with
        | Some pref when pref.Enabled ->
            {
                ShouldSend = true
                Channel = pref.PreferredChannel
                Reason = "Exact match found and enabled"
            }
        | Some pref ->
            {
                ShouldSend = false
                Channel = ""
                Reason = "Exact match found but disabled"
            }
        | None ->
            // Check for category match
            let categoryMatch = 
                preferences 
                |> List.tryFind (fun p -> p.NotificationType.Contains(notificationType.Category.Split('_')[0]))
            
            match categoryMatch with
            | Some pref when pref.Enabled ->
                {
                    ShouldSend = true
                    Channel = pref.PreferredChannel
                    Reason = "Category match found"
                }
            | _ ->
                {
                    ShouldSend = true
                    Channel = "InApp"  // Default to InApp
                    Reason = "No specific preference, using default"
                }

    // Match preference using AI for intelligent matching
    let matchPreferenceWithAI (aiService: IAIService) (notificationType: NotificationType) (preferences: UserPreference list) (userContext: string) : Async<MatchResult> =
        async {
            let preferencesText = preferences |> List.map (fun p -> sprintf "%s: %b, channel %s" p.NotificationType p.Enabled p.PreferredChannel) |> String.concat "; "
            
            let prompt = sprintf "Match notification preference: type %s (%s), preferences [%s], context '%s'. Return JSON: {\"shouldSend\": bool, \"channel\": string, \"reason\": string}" notificationType.Category notificationType.Priority preferencesText userContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcShouldSend() = 
                match preferences |> List.tryFind (fun p -> p.NotificationType = notificationType.Category) with
                | Some pref -> pref.Enabled
                | None -> true
            let shouldSend = try root.GetProperty("shouldSend").GetBoolean() with _ -> calcShouldSend()
            
            let calcChannel() = 
                match preferences |> List.tryFind (fun p -> p.NotificationType = notificationType.Category) with
                | Some pref -> pref.PreferredChannel
                | None -> "InApp"
            let channel = try root.GetProperty("channel").GetString() with _ -> calcChannel()
            
            let reason = try root.GetProperty("reason").GetString() with _ -> "AI-based preference matching"
            
            return {
                ShouldSend = shouldSend
                Channel = channel
                Reason = reason
            }
        }

// Channel Optimizer
module ChannelOptimizer =

    type ChannelPerformance = {
        Channel: string
        DeliveryRate: float
        OpenRate: float
        ClickRate: float
    }

    type ChannelRecommendation = {
        Channel: string
        Score: float
        Reason: string
    }

    // Calculate channel performance score
    let calculateChannelScore (performance: ChannelPerformance) : float =
        let deliveryWeight = 0.4
        let openWeight = 0.4
        let clickWeight = 0.2
        
        performance.DeliveryRate * deliveryWeight +
        performance.OpenRate * openWeight +
        performance.ClickRate * clickWeight

    // Recommend best channel for notification
    let recommendChannel (performances: ChannelPerformance list) (userPreferences: string list) : ChannelRecommendation =
        let availableChannels = 
            performances 
            |> List.filter (fun p -> List.contains p.Channel userPreferences)
        
        if availableChannels.IsEmpty then
            {
                Channel = "InApp"
                Score = 0.5
                Reason = "No available channels, using default"
            }
        else
            let scored = 
                availableChannels 
                |> List.map (fun p -> 
                    {
                        Channel = p.Channel
                        Score = calculateChannelScore p
                        Reason = sprintf "Performance score: %.2f" (calculateChannelScore p)
                    })
            
            scored |> List.maxBy (fun r -> r.Score)

    // Recommend channel using AI for intelligent channel selection
    let recommendChannelWithAI (aiService: IAIService) (performances: ChannelPerformance list) (userPreferences: string list) (notificationContext: string) : Async<ChannelRecommendation> =
        async {
            let performancesText = performances |> List.map (fun p -> sprintf "%s: delivery %.2f, open %.2f, click %.2f" p.Channel p.DeliveryRate p.OpenRate p.ClickRate) |> String.concat "; "
            let preferencesText = String.concat ", " userPreferences
            
            let prompt = sprintf "Recommend channel: performances [%s], user prefs [%s], context '%s'. Return JSON: {\"channel\": string, \"score\": number (0-1), \"reason\": string}" performancesText preferencesText notificationContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let availableChannels = performances |> List.filter (fun p -> List.contains p.Channel userPreferences)
            let calcChannel() = if availableChannels.IsEmpty then "InApp" else availableChannels |> List.maxBy (fun p -> calculateChannelScore p) |> fun p -> p.Channel
            let channel = try root.GetProperty("channel").GetString() with _ -> calcChannel()
            
            let calcScore() = if availableChannels.IsEmpty then 0.5 else availableChannels |> List.map (fun p -> calculateChannelScore p) |> List.max
            let score = try root.GetProperty("score").GetDouble() with _ -> calcScore()
            
            let reason = try root.GetProperty("reason").GetString() with _ -> "AI-based channel recommendation"
            
            return {
                Channel = channel
                Score = score
                Reason = reason
            }
        }

// Frequency Controller
module NotificationFrequencyController =

    type NotificationHistory = {
        Type: string
        Timestamp: DateTime
        Channel: string
    }

    type FrequencyCheck = {
        Allowed: bool
        Reason: string
        RecommendedDelay: TimeSpan option
    }

    // Check if notification should be sent based on frequency limits
    let checkFrequency (notificationType: string) (history: NotificationHistory list) (maxPerHour: int) (maxPerDay: int) : FrequencyCheck =
        let now = DateTime.UtcNow
        let oneHourAgo = now.AddHours(-1.0)
        let oneDayAgo = now.AddDays(-1.0)
        
        let recentHour = 
            history
            |> List.filter (fun h -> h.Type = notificationType && h.Timestamp > oneHourAgo)
        
        let recentDay = 
            history
            |> List.filter (fun h -> h.Type = notificationType && h.Timestamp > oneDayAgo)
        
        if recentHour.Length >= maxPerHour then
            {
                Allowed = false
                Reason = sprintf "Hourly limit exceeded (%d/%d)" recentHour.Length maxPerHour
                RecommendedDelay = Some (TimeSpan.FromMinutes(60.0))
            }
        elif recentDay.Length >= maxPerDay then
            {
                Allowed = false
                Reason = sprintf "Daily limit exceeded (%d/%d)" recentDay.Length maxPerDay
                RecommendedDelay = Some (TimeSpan.FromHours(24.0))
            }
        else
            {
                Allowed = true
                Reason = "Within frequency limits"
                RecommendedDelay = None
            }

    // Calculate optimal send time based on user activity patterns
    let calculateOptimalSendTime (history: NotificationHistory list) : DateTime =
        if history.IsEmpty then
            DateTime.UtcNow
        else
            // Group by hour to find most active time
            let hourCounts = 
                history
                |> List.groupBy (fun h -> h.Timestamp.Hour)
                |> List.map (fun (hour, items) -> (hour, List.length items))
            
            if hourCounts.IsEmpty then
                DateTime.UtcNow
            else
                let (bestHour, _) = hourCounts |> List.maxBy snd
                let now = DateTime.UtcNow
                let targetTime = DateTime(now.Year, now.Month, now.Day, bestHour, 0, 0)
                
                if targetTime > now then
                    targetTime
                else
                    targetTime.AddDays(1.0)

    // Check frequency using AI for intelligent frequency control
    let checkFrequencyWithAI (aiService: IAIService) (notificationType: string) (history: NotificationHistory list) (maxPerHour: int) (maxPerDay: int) (frequencyContext: string) : Async<FrequencyCheck> =
        async {
            let historyText = history |> List.map (fun h -> sprintf "%s at %s via %s" h.Type (h.Timestamp.ToString("o")) h.Channel) |> String.concat "; "
            
            let prompt = sprintf "Check notification frequency: type '%s', history [%s], limits %d/hour %d/day, context '%s'. Return JSON: {\"allowed\": bool, \"reason\": string}" notificationType historyText maxPerHour maxPerDay frequencyContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let now = DateTime.UtcNow
            let oneHourAgo = now.AddHours(-1.0)
            let oneDayAgo = now.AddDays(-1.0)
            let recentHour = history |> List.filter (fun h -> h.Type = notificationType && h.Timestamp > oneHourAgo)
            let recentDay = history |> List.filter (fun h -> h.Type = notificationType && h.Timestamp > oneDayAgo)
            
            let calcAllowed() = recentHour.Length < maxPerHour && recentDay.Length < maxPerDay
            let allowed = try root.GetProperty("allowed").GetBoolean() with _ -> calcAllowed()
            
            let calcReason() = 
                if recentHour.Length >= maxPerHour then sprintf "Hourly limit exceeded (%d/%d)" recentHour.Length maxPerHour
                elif recentDay.Length >= maxPerDay then sprintf "Daily limit exceeded (%d/%d)" recentDay.Length maxPerDay
                else "Within frequency limits"
            let reason = try root.GetProperty("reason").GetString() with _ -> calcReason()
            
            let recommendedDelay = 
                if allowed then None
                elif reason.Contains("Hourly") then Some (TimeSpan.FromMinutes(60.0))
                elif reason.Contains("Daily") then Some (TimeSpan.FromHours(24.0))
                else None
            
            return {
                Allowed = allowed
                Reason = reason
                RecommendedDelay = recommendedDelay
            }
        }
