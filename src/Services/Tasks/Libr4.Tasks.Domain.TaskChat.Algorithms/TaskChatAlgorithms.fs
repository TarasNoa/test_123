namespace Libr4.Tasks.Domain.TaskChat.Algorithms

open System
open System.Text.Json
open Libr4.Tasks.Domain.TaskChat
open Libr4.AI.Application.Abstractions

type TaskChatMessage = Libr4.Tasks.Domain.TaskChat.ChatMessage

// Message Filter
module MessageFilter =

    type FilteredMessages = {
        All: TaskChatMessage list
        BySender: Map<Guid, TaskChatMessage list>
        ByRole: Map<string, TaskChatMessage list>
        Recent: TaskChatMessage list
    }

    // Filter and organize chat messages
    let filterMessages (messages: TaskChatMessage list) (senderId: Guid option) (role: string option) (recentHours: int) : FilteredMessages =
        let bySender = 
            messages
            |> List.groupBy (fun m -> m.SenderId)
            |> Map.ofList
        
        let byRole = 
            messages
            |> List.groupBy (fun m -> m.SenderRole)
            |> Map.ofList
        
        let recent = 
            let cutoff = DateTimeOffset.UtcNow.AddHours(-float recentHours)
            messages
            |> List.filter (fun m -> m.SentAt >= cutoff)
        
        let filtered = 
            match senderId, role with
            | Some sid, Some r -> 
                messages |> List.filter (fun m -> m.SenderId = sid && m.SenderRole = r)
            | Some sid, None -> 
                messages |> List.filter (fun m -> m.SenderId = sid)
            | None, Some r -> 
                messages |> List.filter (fun m -> m.SenderRole = r)
            | None, None -> 
                messages
        
        {
            All = filtered
            BySender = bySender
            ByRole = byRole
            Recent = recent
        }

// Activity Tracker
module ActivityTracker =

    type ActivityMetrics = {
        TotalMessages: int
        MessagesPerDay: float32
        MostActiveSender: Guid option
        LastActivity: DateTimeOffset option
        EngagementScore: float32
    }

    // Track chat activity metrics using AI
    let trackActivity (aiService: IAIService) (messages: TaskChatMessage list) (createdAt: DateTimeOffset) : Async<ActivityMetrics> =
        async {
            let totalMessages = messages.Length
            
            let messagesPerDay = 
                let daysSinceCreation = max 1 (DateTimeOffset.UtcNow - createdAt).Days
                float32 totalMessages / float32 daysSinceCreation
            
            let mostActiveSender = 
                if messages.IsEmpty then None
                else
                    messages
                    |> List.groupBy (fun m -> m.SenderId)
                    |> List.maxBy (fun (_, msgs) -> msgs.Length)
                    |> fst
                    |> Some
            
            let lastActivity = 
                if messages.IsEmpty then None
                else messages |> List.maxBy (fun m -> m.SentAt) |> (fun m -> Some m.SentAt)
            
            let prompt = sprintf "Calculate engagement score for chat with %d total messages, %.1f messages per day. Return JSON: {\"engagementScore\": number (0-100)}" totalMessages messagesPerDay
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse : string)
            let root = jsonDoc.RootElement
            
            let engagementScore = 
                try root.GetProperty("engagementScore").GetSingle()
                with _ ->
                    let volumeScore = float32 totalMessages * 2f |> min 30f
                    let frequencyScore = messagesPerDay * 5f |> min 40f
                    let recencyScore = 
                        match lastActivity with
                        | Some la when (DateTimeOffset.UtcNow - la).TotalHours < 24.0 -> 30f
                        | Some la when (DateTimeOffset.UtcNow - la).TotalHours < 72.0 -> 20f
                        | Some la when (DateTimeOffset.UtcNow - la).TotalHours < 168.0 -> 10f
                        | _ -> 0f
                    volumeScore + frequencyScore + recencyScore |> min 100f
            
            return {
                TotalMessages = totalMessages
                MessagesPerDay = messagesPerDay
                MostActiveSender = mostActiveSender
                LastActivity = lastActivity
                EngagementScore = engagementScore
            }
        }

// Chat Analytics
module ChatAnalytics =

    type ChatInsight = {
        ResponseTime: float32 option // average hours
        MessageLength: float32 // average characters
        ParticipationBalance: float32 // 0-100, 50 = balanced
        Recommendation: string
    }

    // Analyze chat patterns and provide insights using AI
    let analyzeChat (aiService: IAIService) (messages: TaskChatMessage list) : Async<ChatInsight> =
        async {
            if messages.IsEmpty then
                return {
                    ResponseTime = None
                    MessageLength = 0f
                    ParticipationBalance = 50f
                    Recommendation = "No messages to analyze"
                }
            else
                let messageLength = 
                    messages
                    |> List.map (fun m -> float32 m.Content.Length)
                    |> List.average
                
                let byRole = 
                    messages
                    |> List.groupBy (fun m -> m.SenderRole)
                    |> List.map (fun (role, msgs) -> (role, float32 msgs.Length))
                
                let participationBalance = 
                    match byRole with
                    | [("client", c); ("freelancer", f)] -> 
                        let total = c + f
                        if total > 0f then (c / total) * 100f else 50f
                    | _ -> 50f
                
                let prompt = sprintf "Analyze chat communication: %.1f avg message length, %.1f%% client participation. Return JSON: {\"recommendation\": string}" messageLength participationBalance
                
                let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
                
                let jsonDoc = JsonDocument.Parse(aiResponse : string)
                let root = jsonDoc.RootElement
                
                let recommendation = 
                    try root.GetProperty("recommendation").GetString()
                    with _ ->
                        if participationBalance < 30f then "Encourage more participation from client"
                        elif participationBalance > 70f then "Encourage more participation from freelancer"
                        elif messageLength < 50f then "Consider encouraging more detailed communication"
                        else "Communication appears balanced"
                
                return {
                    ResponseTime = None
                    MessageLength = messageLength
                    ParticipationBalance = participationBalance
                    Recommendation = recommendation
                }
        }
