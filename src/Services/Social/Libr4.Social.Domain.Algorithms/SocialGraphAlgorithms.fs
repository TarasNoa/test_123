namespace Libr4.Social.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Infrastructure.AI

// Shared types for social graph
type GraphEdge = {
    Source: Guid
    Target: Guid
    Weight: float
}

type SocialGraph = {
    Nodes: Set<Guid>
    Edges: GraphEdge list
}

// Social Graph Analysis Algorithms
module SocialGraphAnalyzer =

    // Calculate degree centrality (number of connections)
    let calculateDegreeCentrality (graph: SocialGraph) (nodeId: Guid) : int =
        graph.Edges
        |> List.filter (fun edge -> edge.Source = nodeId || edge.Target = nodeId)
        |> List.length

    // Calculate betweenness centrality (how often a node lies on shortest paths)
    let calculateBetweennessCentrality (graph: SocialGraph) (nodeId: Guid) : float =
        // Simplified betweenness centrality calculation
        let directConnections = 
            graph.Edges
            |> List.filter (fun edge -> edge.Source = nodeId || edge.Target = nodeId)
            |> List.length
        
        let totalConnections = List.length graph.Edges
        if totalConnections = 0 then 0.0
        else float directConnections / float totalConnections

    // Detect communities using simple clustering
    let detectCommunities (graph: SocialGraph) : (Guid list) list =
        // Simplified community detection using connected components
        let mutable visited = Set.empty
        let mutable communities = []
        
        for node in graph.Nodes do
            if not (Set.contains node visited) then
                let mutable cluster = []
                let mutable queue = [node]
                let mutable visitedInCluster = Set.add node visited
                
                while queue <> [] do
                    let current = List.head queue
                    queue <- List.tail queue
                    cluster <- current :: cluster
                    
                    // Find neighbors
                    let neighbors = 
                        graph.Edges
                        |> List.filter (fun edge -> edge.Source = current)
                        |> List.map (fun edge -> edge.Target)
                    
                    for neighbor in neighbors do
                        if not (Set.contains neighbor visitedInCluster) then
                            queue <- queue @ [neighbor]
                            visitedInCluster <- Set.add neighbor visitedInCluster
                
                visited <- Set.union visited visitedInCluster
                communities <- cluster :: communities
        
        communities

    // Find mutual friends between two users
    let findMutualFriends (graph: SocialGraph) (user1: Guid) (user2: Guid) : Guid list =
        let user1Connections = 
            graph.Edges
            |> List.filter (fun edge -> edge.Source = user1)
            |> List.map (fun edge -> edge.Target)
            |> Set.ofList
        
        let user2Connections = 
            graph.Edges
            |> List.filter (fun edge -> edge.Source = user2)
            |> List.map (fun edge -> edge.Target)
            |> Set.ofList
        
        Set.intersect user1Connections user2Connections
        |> Set.toList

    // Detect communities using AI for intelligent community detection
    let detectCommunitiesWithAI (aiService: IAIService) (graph: SocialGraph) (communityContext: string) : Async<(Guid list) list> =
        async {
            let nodesText = graph.Nodes |> Set.toList |> List.map string |> String.concat "; "
            let edgesText = graph.Edges |> List.map (fun e -> sprintf "%s->%s" (string e.Source) (string e.Target)) |> String.concat "; "
            
            let prompt = sprintf "Detect communities from nodes [%s] and edges [%s], context '%s'. Return JSON: {\"communities\": [[string]]}" nodesText edgesText communityContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "social") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let communities = try root.GetProperty("communities").EnumerateArray() |> Seq.map (fun community -> community.EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> List.ofSeq) |> List.ofSeq with _ -> []
            
            if communities.IsEmpty then
                return detectCommunities graph
            else
                return communities
        }

// Recommendation Algorithms
module SocialRecommender =

    type UserProfile = {
        Id: Guid
        Interests: string list
        Connections: Guid list
        ActivityScore: float
    }

    // Recommend friends based on mutual connections and interests
    let recommendFriends (currentUser: UserProfile) (allUsers: UserProfile list) (graph: SocialGraph) : (Guid * float) list =
        allUsers
        |> List.filter (fun user -> user.Id <> currentUser.Id)
        |> List.map (fun user ->
            let mutualCount = 
                SocialGraphAnalyzer.findMutualFriends graph currentUser.Id user.Id
                |> List.length
            
            let commonInterests = 
                Set.intersect (Set.ofList currentUser.Interests) (Set.ofList user.Interests)
                |> Set.toList
                |> List.length
            
            let activityScore = user.ActivityScore
            
            // Calculate recommendation score
            let score = float mutualCount * 0.5 + float commonInterests * 0.3 + activityScore * 0.2
            (user.Id, score))
        |> List.filter (fun (_, score) -> score > 0.5)
        |> List.sortByDescending snd
        |> List.take 10

    // Recommend content based on interests and social graph
    let recommendContent (currentUser: UserProfile) (friends: UserProfile list) (content: (Guid * string list) list) : (Guid * float) list =
        let friendInterests = 
            friends
            |> List.collect (fun friend -> friend.Interests)
        
        let userInterests = currentUser.Interests @ friendInterests
        
        content
        |> List.map (fun (contentId, contentTags) ->
            let matchCount = 
                Set.intersect (Set.ofList userInterests) (Set.ofList contentTags)
                |> Set.toList
                |> List.length
            
            let score = float matchCount / float (List.length contentTags + 1)
            (contentId, score))
        |> List.filter (fun (_, score) -> score > 0.0)
        |> List.sortByDescending snd
        |> List.take 20

    // Recommend friends using AI for intelligent matching
    let recommendFriendsWithAI (aiService: IAIService) (currentUser: UserProfile) (allUsers: UserProfile list) (graph: SocialGraph) (recommendationContext: string) : Async<(Guid * float) list> =
        async {
            let userInterestsText = currentUser.Interests |> String.concat ", "
            let allUsersText = allUsers |> List.map (fun u -> sprintf "%s: %s" (string u.Id) (String.concat ", " u.Interests)) |> String.concat "; "
            
            let prompt = sprintf "Recommend friends for user with interests [%s] from users [%s], context '%s'. Return JSON: {\"recommendedUserIds\": [string]}" userInterestsText allUsersText recommendationContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "social") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let recommendedIds = try root.GetProperty("recommendedUserIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> Set.ofSeq with _ -> Set.empty
            
            if recommendedIds.IsEmpty then
                return recommendFriends currentUser allUsers graph
            else
                return allUsers
                    |> List.filter (fun user -> Set.contains user.Id recommendedIds)
                    |> List.map (fun user -> (user.Id, 0.8))
                    |> List.sortByDescending snd
                    |> List.take 10
        }

    // Recommend content using AI for intelligent content matching
    let recommendContentWithAI (aiService: IAIService) (currentUser: UserProfile) (friends: UserProfile list) (content: (Guid * string list) list) (contentContext: string) : Async<(Guid * float) list> =
        async {
            let userInterestsText = currentUser.Interests |> String.concat ", "
            let friendInterestsText = friends |> List.collect (fun f -> f.Interests) |> String.concat ", "
            let contentText = content |> List.map (fun (id, tags) -> sprintf "%s: %s" (string id) (String.concat ", " tags)) |> String.concat "; "
            
            let prompt = sprintf "Recommend content for user with interests [%s] and friend interests [%s] from content [%s], context '%s'. Return JSON: {\"recommendedContentIds\": [string]}" userInterestsText friendInterestsText contentText contentContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "social") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let recommendedIds = try root.GetProperty("recommendedContentIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> Set.ofSeq with _ -> Set.empty
            
            if recommendedIds.IsEmpty then
                return recommendContent currentUser friends content
            else
                return content
                    |> List.filter (fun (id, _) -> Set.contains id recommendedIds)
                    |> List.map (fun (id, _) -> (id, 0.8))
                    |> List.sortByDescending snd
                    |> List.take 20
        }

// Influence Analysis Algorithms
module InfluenceAnalyzer =

    type InfluenceMetrics = {
        Reach: int
        EngagementRate: float
        ViralCoefficient: float
    }

    // Calculate influence score based on network position
    let calculateInfluenceScore (graph: SocialGraph) (nodeId: Guid) (followersCount: int) (engagementRate: float) : float =
        let centrality = SocialGraphAnalyzer.calculateDegreeCentrality graph nodeId
        let betweenness = SocialGraphAnalyzer.calculateBetweennessCentrality graph nodeId
        
        // Weighted influence score
        let networkScore = float centrality * 0.4 + betweenness * 0.3
        let engagementScore = engagementRate * 0.3
        
        networkScore + engagementScore

    // Identify influencers in the network
    let identifyInfluencers (graph: SocialGraph) (userMetrics: Map<Guid, InfluenceMetrics>) : (Guid * float) list =
        userMetrics
        |> Map.toList
        |> List.map (fun (userId, metrics) ->
            let influenceScore = calculateInfluenceScore graph userId metrics.Reach metrics.EngagementRate
            (userId, influenceScore))
        |> List.filter (fun (_, score) -> score > 0.5)
        |> List.sortByDescending snd
        |> List.take 10

    // Identify influencers using AI for intelligent influence detection
    let identifyInfluencersWithAI (aiService: IAIService) (graph: SocialGraph) (userMetrics: Map<Guid, InfluenceMetrics>) (influenceContext: string) : Async<(Guid * float) list> =
        async {
            let metricsText = userMetrics |> Map.toList |> List.map (fun (id, m) -> sprintf "%s: reach %d, engagement %.1f%%" (string id) m.Reach m.EngagementRate) |> String.concat "; "
            
            let prompt = sprintf "Identify top influencers from metrics [%s], context '%s'. Return JSON: {\"influencerIds\": [string]}" metricsText influenceContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "social") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let influencerIds = try root.GetProperty("influencerIds").EnumerateArray() |> Seq.map (fun id -> Guid(id.GetString())) |> Set.ofSeq with _ -> Set.empty
            
            if influencerIds.IsEmpty then
                return identifyInfluencers graph userMetrics
            else
                return userMetrics
                    |> Map.toList
                    |> List.filter (fun (id, _) -> Set.contains id influencerIds)
                    |> List.map (fun (id, _) -> (id, 0.9))
                    |> List.sortByDescending snd
                    |> List.take 10
        }

// Activity Analysis Algorithms
module ActivityAnalyzer =

    type ActivityEvent = {
        UserId: Guid
        Timestamp: DateTime
        EventType: string
    }

    // Calculate user activity level over time period
    let calculateActivityLevel (events: ActivityEvent list) (days: int) : float =
        let now = DateTime.UtcNow
        let cutoff = now.AddDays(-float days)
        
        let recentEvents = 
            events
            |> List.filter (fun event -> event.Timestamp >= cutoff)
        
        if recentEvents.IsEmpty then 0.0
        else float recentEvents.Length / float days

    // Detect activity patterns (e.g., most active time of day)
    let detectActivityPatterns (events: ActivityEvent list) : Map<int, int> list =
        let hourlyActivity = 
            events
            |> List.groupBy (fun event -> event.Timestamp.Hour)
            |> List.map (fun (hour, events) -> hour, List.length events)
            |> Map.ofList
        
        [hourlyActivity]

    // Calculate engagement rate
    let calculateEngagementRate (likes: int) (comments: int) (views: int) : float =
        if views = 0 then 0.0
        else float (likes + comments * 2) / float views * 100.0
