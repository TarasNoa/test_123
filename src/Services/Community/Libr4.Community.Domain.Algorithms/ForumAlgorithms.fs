namespace Libr4.Community.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Application.Abstractions

// Content Moderation Algorithms
module ContentModerator =

    type ModerationResult = {
        IsApproved: bool
        ModerationScore: float
        Flags: string list
        Reason: string
    }

    // Analyze content for moderation using AI
    let moderateContentWithAI (aiService: IAIService) (content: string) (contentType: string) (moderationContext: string) : Async<ModerationResult> =
        async {
            let prompt = sprintf "Moderate %s content: '%s', context '%s'. Return JSON: {\"isApproved\": boolean, \"moderationScore\": number, \"flags\": [string], \"reason\": string}" contentType content moderationContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "community") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let isApproved = try root.GetProperty("isApproved").GetBoolean() with _ -> true
            let moderationScore = try root.GetProperty("moderationScore").GetDouble() with _ -> 1.0
            let flags = try root.GetProperty("flags").EnumerateArray() |> Seq.map (fun f -> f.GetString()) |> List.ofSeq with _ -> []
            let reason = try root.GetProperty("reason").GetString() with _ -> "Content approved"
            
            return {
                IsApproved = isApproved
                ModerationScore = moderationScore
                Flags = flags
                Reason = reason
            }
        }

    // Detect spam using heuristic analysis
    let detectSpam (content: string) (authorHistory: float) : bool =
        let spamKeywords = ["buy now"; "free"; "click here"; "winner"; "lottery"; "casino"; "viagra"; "porn"]
        let lowerContent = content.ToLower()
        
        let keywordSpam = spamKeywords |> List.exists (fun keyword -> lowerContent.Contains(keyword))
        let excessiveCaps = content |> Seq.filter Char.IsUpper |> Seq.length > (content.Length / 2)
        let excessiveLinks = content.Split([|' '|]) |> Array.filter (fun word -> word.Contains("http")) |> Array.length > 3
        
        let spamScore = 
            (if keywordSpam then 1 else 0) +
            (if excessiveCaps then 1 else 0) +
            (if excessiveLinks then 1 else 0)
        
        spamScore >= 2 || authorHistory < 0.3

// Topic Recommendation Algorithms
module TopicRecommender =

    type TopicRelevanceScore = {
        TopicId: Guid
        RelevanceScore: float
        Reason: string
    }

    // Recommend topics using AI for intelligent matching
    let recommendTopicsWithAI (aiService: IAIService) (userInterests: string list) (allTopics: (Guid * string * string) list) (recommendationContext: string) : Async<TopicRelevanceScore list> =
        async {
            let interestsText = userInterests |> String.concat ", "
            let topicsText = allTopics |> List.map (fun (id, title, content) -> sprintf "%s: %s - %s" (string id) title content) |> String.concat "; "
            
            let prompt = sprintf "Recommend topics for user with interests [%s] from topics [%s], context '%s'. Return JSON: {\"recommendedTopicIds\": [string], \"reasons\": [string]}" interestsText topicsText recommendationContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "community") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let recommendedIds = try root.GetProperty("recommendedTopicIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> Set.ofSeq with _ -> Set.empty
            let reasons = try root.GetProperty("reasons").EnumerateArray() |> Seq.map (fun r -> r.GetString()) |> List.ofSeq with _ -> []
            
            if recommendedIds.IsEmpty then
                return []
            else
                return allTopics
                    |> List.filter (fun (id, _, _) -> Set.contains id recommendedIds)
                    |> List.mapi (fun i (id, _, _) -> 
                        {
                            TopicId = id
                            RelevanceScore = 0.9 - (float i * 0.05)
                            Reason = if i < reasons.Length then reasons.[i] else "Topic matches interests"
                        })
                    |> List.sortByDescending (fun t -> t.RelevanceScore)
                    |> List.take 10
        }

    // Calculate topic relevance based on keywords
    let calculateTopicRelevance (userInterests: string list) (topicTitle: string) (topicContent: string) : float =
        let combinedText = (topicTitle + " " + topicContent).ToLower()
        let matchedInterests = userInterests |> List.filter (fun interest -> combinedText.Contains(interest.ToLower()))
        
        if matchedInterests.IsEmpty then 0.0
        else float matchedInterests.Length / float userInterests.Length

// Activity Analysis Algorithms
module ActivityAnalyzer =

    type ActivityMetrics = {
        ActiveUsers: int
        NewTopics: int
        NewPosts: int
        EngagementRate: float
        Trend: string // increasing, stable, decreasing
    }

    // Analyze forum activity using AI
    let analyzeActivityWithAI (aiService: IAIService) (topicCount: int) (postCount: int) (activeUsers: int) (previousMetrics: ActivityMetrics option) (activityContext: string) : Async<ActivityMetrics> =
        async {
            let prevText = match previousMetrics with
                | Some m -> sprintf "previous: active %d, topics %d, posts %d, engagement %.1f%%" m.ActiveUsers m.NewTopics m.NewPosts m.EngagementRate
                | None -> "no previous data"
            
            let prompt = sprintf "Analyze forum activity: topics %d, posts %d, active users %d, %s, context '%s'. Return JSON: {\"activeUsers\": number, \"newTopics\": number, \"newPosts\": number, \"engagementRate\": number, \"trend\": string}" topicCount postCount activeUsers prevText activityContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "community") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcFallback() =
                let engagementRate = if postCount > 0 then float (activeUsers * 100) / float postCount else 0.0
                let trend = match previousMetrics with
                    | Some prev -> if engagementRate > prev.EngagementRate + 5.0 then "increasing" elif engagementRate < prev.EngagementRate - 5.0 then "decreasing" else "stable"
                    | None -> "stable"
                {
                    ActiveUsers = activeUsers
                    NewTopics = topicCount
                    NewPosts = postCount
                    EngagementRate = engagementRate
                    Trend = trend
                }
            
            let activeUsers = try root.GetProperty("activeUsers").GetInt32() with _ -> calcFallback().ActiveUsers
            let newTopics = try root.GetProperty("newTopics").GetInt32() with _ -> calcFallback().NewTopics
            let newPosts = try root.GetProperty("newPosts").GetInt32() with _ -> calcFallback().NewPosts
            let engagementRate = try root.GetProperty("engagementRate").GetDouble() with _ -> calcFallback().EngagementRate
            let trend = try root.GetProperty("trend").GetString() with _ -> calcFallback().Trend
            
            return {
                ActiveUsers = activeUsers
                NewTopics = newTopics
                NewPosts = newPosts
                EngagementRate = engagementRate
                Trend = trend
            }
        }

// Search Algorithms
module SearchEngine =

    type SearchResult = {
        TopicId: Guid
        Score: float
        Snippet: string
    }

    // Search topics using AI for intelligent matching
    let searchTopicsWithAI (aiService: IAIService) (searchQuery: string) (allTopics: (Guid * string * string) list) (searchContext: string) : Async<SearchResult list> =
        async {
            let topicsText = allTopics |> List.map (fun (id, title, content) -> sprintf "%s: %s - %s" (string id) title content) |> String.concat "; "
            
            let prompt = sprintf "Search for '%s' in topics [%s], context '%s'. Return JSON: {\"matchingTopicIds\": [string], \"snippets\": [string]}" searchQuery topicsText searchContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "community") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let matchingIds = try root.GetProperty("matchingTopicIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> Set.ofSeq with _ -> Set.empty
            let snippets = try root.GetProperty("snippets").EnumerateArray() |> Seq.map (fun s -> s.GetString()) |> List.ofSeq with _ -> []
            
            if matchingIds.IsEmpty then
                return []
            else
                return allTopics
                    |> List.filter (fun (id, _, _) -> Set.contains id matchingIds)
                    |> List.mapi (fun i (id, title, content) ->
                        let snippet = if i < snippets.Length then snippets.[i] else title
                        {
                            TopicId = id
                            Score = 0.9 - (float i * 0.05)
                            Snippet = snippet
                        })
                    |> List.sortByDescending (fun r -> r.Score)
                    |> List.take 20
        }

    // Simple keyword-based search
    let searchTopics (searchQuery: string) (allTopics: (Guid * string * string) list) : SearchResult list =
        let queryWords = searchQuery.ToLower().Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
        
        allTopics
        |> List.map (fun (id, title, content) ->
            let combinedText = (title + " " + content).ToLower()
            let matchedWords = queryWords |> Array.filter (fun word -> combinedText.Contains(word))
            let score = if matchedWords.Length > 0 then float matchedWords.Length / float queryWords.Length else 0.0
            {
                TopicId = id
                Score = score
                Snippet = title
            })
        |> List.filter (fun r -> r.Score > 0.0)
        |> List.sortByDescending (fun r -> r.Score)
        |> List.take 20
