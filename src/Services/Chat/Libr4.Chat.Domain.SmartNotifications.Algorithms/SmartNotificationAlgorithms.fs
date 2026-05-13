namespace Libr4.Chat.Domain.SmartNotifications.Algorithms

open System
open System.Text.Json
open Libr4.AI.Application.Abstractions

// Notification Prioritization Algorithms
module NotificationPrioritizer =

    type NotificationPriority = {
        UserId: Guid
        NotificationId: Guid
        Type: string
        Urgency: float
        Relevance: float
        UserPreference: float
    }

    // Calculate notification priority based on multiple factors
    let calculatePriority (urgency: float) (relevance: float) (userPreference: float) : float =
        // Weighted combination of factors
        urgency * 0.4 + relevance * 0.4 + userPreference * 0.2

    // Prioritize notifications for a user
    let prioritizeNotifications (notifications: NotificationPriority list) : NotificationPriority list =
        notifications
        |> List.map (fun n ->
            let priority = calculatePriority n.Urgency n.Relevance n.UserPreference
            { n with Urgency = priority })
        |> List.sortByDescending (fun n -> n.Urgency)

    // Calculate priority using AI for intelligent scoring
    let calculatePriorityWithAI (aiService: IAIService) (notifications: NotificationPriority list) (userContext: string) : Async<NotificationPriority list> =
        async {
            let notificationsText = notifications |> List.map (fun n -> sprintf "Type %s, urgency %.2f, relevance %.2f, preference %.2f" n.Type n.Urgency n.Relevance n.UserPreference) |> String.concat "; "
            
            let prompt = sprintf "Calculate AI priority for notifications: [%s], user context '%s'. Return JSON: {\"priorities\": [{\"notificationId\": string (guid), \"aiPriority\": number (0-1)}]}" notificationsText userContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let priorityMap = 
                try
                    root.GetProperty("priorities").EnumerateArray()
                    |> Seq.map (fun p ->
                        let id = Guid.Parse(p.GetProperty("notificationId").GetString())
                        let aiPriority = p.GetProperty("aiPriority").GetDouble()
                        (id, aiPriority))
                    |> Map.ofSeq
                with _ -> Map.empty
            
            let result =
                notifications
                |> List.map (fun n ->
                    let aiPriority = Map.tryFind n.NotificationId priorityMap |> Option.defaultValue n.Urgency
                    { n with Urgency = aiPriority })
                |> List.sortByDescending (fun n -> n.Urgency)
            
            return result
        }

// Notification Routing Algorithms
module NotificationRouter =

    type RoutingRule = {
        NotificationType: string
        Channels: string list
        Conditions: Map<string, string>
    }

    type DeliveryChannel = {
        Name: string
        IsEnabled: bool
        Priority: int
    }

    // Determine delivery channels for a notification
    let determineChannels (notificationType: string) (rules: RoutingRule list) (userChannels: DeliveryChannel list) : string list =
        let matchingRule = 
            rules
            |> List.tryFind (fun r -> r.NotificationType = notificationType)
        
        match matchingRule with
        | None -> ["Push"] // Default to push
        | Some rule ->
            rule.Channels
            |> List.filter (fun channel ->
                userChannels
                |> List.exists (fun uc -> uc.Name = channel && uc.IsEnabled))

    // Determine channels using AI for intelligent routing
    let determineChannelsWithAI (aiService: IAIService) (notificationType: string) (userChannels: DeliveryChannel list) (notificationContext: string) : Async<string list> =
        async {
            let channelsText = userChannels |> List.map (fun c -> sprintf "%s (enabled: %b, priority: %d)" c.Name c.IsEnabled c.Priority) |> String.concat ", "
            
            let prompt = sprintf "Determine best delivery channels for notification type '%s', available channels [%s], context '%s'. Return JSON: {\"channels\": [string]}" notificationType channelsText notificationContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let channels = 
                try
                    root.GetProperty("channels").EnumerateArray()
                    |> Seq.map (fun c -> c.GetString())
                    |> List.ofSeq
                with _ ->
                    userChannels |> List.filter (fun uc -> uc.IsEnabled) |> List.map (fun uc -> uc.Name)
            
            return channels |> List.filter (fun channel -> userChannels |> List.exists (fun uc -> uc.Name = channel && uc.IsEnabled))
        }

// Smart Notification Aggregation
module NotificationAggregator =

    type AggregatedNotification = {
        NotificationIds: Guid list
        Type: string
        Count: int
        FirstTimestamp: DateTime
        LastTimestamp: DateTime
    }

    // Aggregate similar notifications
    let aggregateNotifications (notifications: (Guid * string * DateTime) list) (timeWindow: TimeSpan) : AggregatedNotification list =
        notifications
        |> List.groupBy (fun (_, type_, _) -> type_)
        |> List.map (fun (type_, grouped) ->
            let sorted = grouped |> List.sortBy (fun (_, _, timestamp) -> timestamp)
            let (_, _, firstTimestamp) = List.head sorted
            let (_, _, lastTimestamp) = List.last sorted
            
            let windowedNotifications = 
                sorted
                |> List.filter (fun (_, _, timestamp) ->
                    (timestamp - firstTimestamp) <= timeWindow)
            
            let notificationIds = windowedNotifications |> List.map (fun (id, _, _) -> id)
            
            {
                NotificationIds = notificationIds
                Type = type_
                Count = List.length windowedNotifications
                FirstTimestamp = firstTimestamp
                LastTimestamp = lastTimestamp
            })
        |> List.filter (fun agg -> agg.Count > 1)

// Notification Frequency Control
module FrequencyController =

    type NotificationHistory = {
        UserId: Guid
        Type: string
        Timestamps: DateTime list
    }

    type FrequencyLimit = {
        MaxPerHour: int
        MaxPerDay: int
        CooldownMinutes: int
    }

    // Check if notification should be sent based on frequency limits
    let shouldSendNotification (history: NotificationHistory) (limits: FrequencyLimit) (now: DateTime) : bool =
        let oneHourAgo = now.AddHours(-1.0)
        let oneDayAgo = now.AddDays(-1.0)
        let cooldownAgo = now.AddMinutes(-float limits.CooldownMinutes)
        
        let recentHour = history.Timestamps |> List.filter (fun t -> t >= oneHourAgo) |> List.length
        let recentDay = history.Timestamps |> List.filter (fun t -> t >= oneDayAgo) |> List.length
        let lastNotification = history.Timestamps |> List.tryLast |> Option.defaultValue DateTime.MinValue
        
        if recentHour >= limits.MaxPerHour then false
        elif recentDay >= limits.MaxPerDay then false
        elif lastNotification >= cooldownAgo then false
        else true

// User Preference Learning
module PreferenceLearner =

    type UserInteraction = {
        NotificationId: Guid
        UserId: Guid
        Action: string  // "viewed", "clicked", "dismissed", "snoozed"
        Timestamp: DateTime
    }

    type UserPreferences = {
        UserId: Guid
        PreferredTypes: Map<string, float>
        PreferredHours: int list
        QuietHoursStart: int
        QuietHoursEnd: int
    }

    // Learn user preferences from interaction history
    let learnPreferences (interactions: UserInteraction list) : UserPreferences =
        let userId = if interactions.IsEmpty then Guid.Empty else (List.head interactions).UserId
        
        let typeScores = 
            interactions
            |> List.groupBy (fun i -> i.Action)
            |> List.map (fun (action, grouped) ->
                let score = 
                    match action with
                    | "clicked" -> 1.0
                    | "viewed" -> 0.5
                    | "dismissed" -> -0.5
                    | "snoozed" -> -0.3
                    | _ -> 0.0
                (action, score))
            |> Map.ofList
        
        let activeHours = 
            interactions
            |> List.map (fun i -> i.Timestamp.Hour)
            |> List.countBy id
            |> List.filter (fun (_, count) -> count > 5)
            |> List.map fst
        
        {
            UserId = userId
            PreferredTypes = typeScores
            PreferredHours = activeHours
            QuietHoursStart = 22 // Default: 10 PM
            QuietHoursEnd = 8   // Default: 8 AM
        }

    // Learn preferences using AI for intelligent pattern recognition
    let learnPreferencesWithAI (aiService: IAIService) (interactions: UserInteraction list) (additionalContext: string) : Async<UserPreferences> =
        async {
            let userId = if interactions.IsEmpty then Guid.Empty else (List.head interactions).UserId
            
            let interactionsText = interactions |> List.map (fun i -> sprintf "Action %s at %s" i.Action (i.Timestamp.ToString("o"))) |> String.concat "; "
            
            let prompt = sprintf "Learn user notification preferences from interactions: [%s], context '%s'. Return JSON: {\"preferredTypes\": {string: number}, \"preferredHours\": [number], \"quietHoursStart\": number, \"quietHoursEnd\": number}" interactionsText additionalContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let preferredTypes = 
                try
                    root.GetProperty("preferredTypes").EnumerateObject()
                    |> Seq.map (fun p -> (p.Name, p.Value.GetDouble()))
                    |> Map.ofSeq
                with _ -> Map.empty
            
            let preferredHours = 
                try
                    root.GetProperty("preferredHours").EnumerateArray()
                    |> Seq.map (fun h -> h.GetInt32())
                    |> List.ofSeq
                with _ ->
                    interactions
                    |> List.map (fun i -> i.Timestamp.Hour)
                    |> List.countBy id
                    |> List.filter (fun (_, count) -> count > 5)
                    |> List.map fst
            
            let quietHoursStart = try root.GetProperty("quietHoursStart").GetInt32() with _ -> 22
            let quietHoursEnd = try root.GetProperty("quietHoursEnd").GetInt32() with _ -> 8
            
            return {
                UserId = userId
                PreferredTypes = preferredTypes
                PreferredHours = preferredHours
                QuietHoursStart = quietHoursStart
                QuietHoursEnd = quietHoursEnd
            }
        }

    // Check if user prefers to receive notifications at current time
    let shouldNotifyNow (preferences: UserPreferences) (currentTime: DateTime) : bool =
        let currentHour = currentTime.Hour
        if currentHour >= preferences.QuietHoursStart || currentHour < preferences.QuietHoursEnd then
            false
        else
            List.contains currentHour preferences.PreferredHours || preferences.PreferredHours.IsEmpty
