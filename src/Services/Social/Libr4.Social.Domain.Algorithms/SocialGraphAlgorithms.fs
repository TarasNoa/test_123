namespace Libr4.Social.Domain.Algorithms

open System
open System.Collections.Generic
open System.Linq

[<CLIMutable>]
type UserNode =
    { UserId: Guid
      Name: string
      ConnectionCount: int
      FollowerCount: int
      FollowingCount: int
      Influence: float
      LastActiveAt: DateTime }

[<CLIMutable>]
type ConnectionEdge =
    { From: Guid
      To: Guid
      Type: string
      Weight: float
      ConnectedAt: DateTime }

[<CLIMutable>]
type CommunityGroup = 
    { Id: int
      Members: Guid list
      Density: float
      InfluenceScore: float }

module SocialGraphAlgorithms =

    /// Calculate influence score based on multiple factors
    let calculateInfluenceScore (followers: int) (postEngagement: int) (connectionQuality: float) : float =
        let followerScore = float followers / 100.0 * 0.3
        let engagementScore = float postEngagement / 50.0 * 0.4
        let qualityScore = connectionQuality * 0.3
        followerScore + engagementScore + qualityScore

    /// Find mutual connections between two users
    let findMutualConnections (user1Connections: Guid list) (user2Connections: Guid list) : Guid list =
        user1Connections
        |> List.filter (fun conn -> List.contains conn user2Connections)

    /// Calculate shortest path between two users using BFS
    let calculateConnectionDistance (adjList: Dictionary<Guid, Guid list>) (start: Guid) (target: Guid) : Option<int> =
        if start = target then Some 0
        else
            let rec bfs queue visited distance =
                match queue with
                | [] -> None
                | node :: rest ->
                    if node = target then Some distance
                    else
                        let neighbors = 
                            match adjList.TryGetValue node with
                            | true, n -> n
                            | false, _ -> []
                        let unvisited = neighbors |> List.filter (fun n -> not (Set.contains n visited))
                        let newVisited = unvisited |> List.fold (fun acc x -> Set.add x acc) visited
                        let newQueue = rest @ unvisited
                        bfs newQueue newVisited (distance + 1)

            bfs [start] (Set.singleton start) 0

    /// Recommend new connections based on mutual connections (2nd degree)
    let recommendConnections (userConnections: Guid list) (connectionsMap: Map<Guid, Guid list>) (topN: int) : (Guid * int) list =
        connectionsMap
        |> Map.fold (fun acc userId secondDegreeConns ->
            let recommendations = 
                secondDegreeConns
                |> List.filter (fun conn -> not (List.contains conn userConnections) && conn <> userId)
            if List.length recommendations > 0 then
                (userId, List.length recommendations) :: acc
            else
                acc
        ) []
        |> List.sortByDescending (fun (_, count) -> count)
        |> List.take (min topN (connectionsMap |> Map.count))

    /// Detect influencers in the network
    let detectInfluencers (users: UserNode list) (threshold: float) : UserNode list =
        users
        |> List.filter (fun user -> user.Influence > threshold)
        |> List.sortByDescending (fun user -> user.Influence * float user.FollowerCount)

    /// Calculate network density (interconnectedness)
    let calculateNetworkDensity (totalUsers: int) (totalConnections: int) : float =
        if totalUsers <= 1 then 0.0
        else
            let maxConnections = (totalUsers * (totalUsers - 1)) / 2
            float totalConnections / float maxConnections

    /// Find key influencers with minimum follower threshold
    let findKeyInfluencers (users: UserNode list) (minFollowers: int) : UserNode list =
        users
        |> List.filter (fun user -> user.FollowerCount >= minFollowers)
        |> List.sortByDescending (fun user -> user.Influence * float user.FollowerCount)

    /// Community detection using simple clustering
    let detectCommunities (edges: ConnectionEdge list) (users: UserNode list) : CommunityGroup list =
        let grouped = 
            users
            |> List.groupBy (fun user -> user.ConnectionCount / 5)
            |> List.map (fun (groupId, userGroup) ->
                let memberIds = userGroup |> List.map (fun u -> u.UserId)
                let totalInfluence = userGroup |> List.sumBy (fun u -> u.Influence)
                let density = 
                    let edgesInGroup = 
                        edges 
                        |> List.filter (fun e -> memberIds |> List.contains e.From && memberIds |> List.contains e.To)
                        |> List.length
                    float edgesInGroup / float (List.length memberIds)

                { Id = groupId
                  Members = memberIds
                  Density = density
                  InfluenceScore = totalInfluence / float (List.length userGroup) }
            )
        grouped

    /// Calculate PageRank-like algorithm for user importance
    let calculatePageRank (edges: ConnectionEdge list) (damping: float) (iterations: int) : Map<Guid, float> =
        let users = 
            edges
            |> List.collect (fun e -> [e.From; e.To])
            |> List.distinct

        let mutable ranks = Map.ofList (users |> List.map (fun u -> (u, 1.0 / float users.Length)))

        for _ = 1 to iterations do
            let newRanks = ref (Map.ofList (users |> List.map (fun u -> (u, 0.0))))
            
            for user in users do
                let incoming = edges |> List.filter (fun e -> e.To = user)
                let contribution = 
                    if List.isEmpty incoming then 0.0
                    else
                        incoming
                        |> List.sumBy (fun e -> 
                            let outgoing = edges |> List.filter (fun x -> x.From = e.From)
                            ranks.[e.From] / float (List.length outgoing + 1)
                        )
                
                let newRank = (1.0 - damping) / float users.Length + damping * contribution
                newRanks := Map.add user newRank !newRanks

            ranks <- !newRanks

        ranks

    /// Find densest subgraph (core group)
    let findDensestSubgraph (edges: ConnectionEdge list) : Guid list =
        let users = 
            edges
            |> List.collect (fun e -> [e.From; e.To])
            |> List.distinct

        users
        |> List.sortByDescending (fun user ->
            edges
            |> List.filter (fun e -> e.From = user || e.To = user)
            |> List.length
        )
        |> List.take (max 1 (List.length users / 3))

    /// Similarity between two users (0.0 - 1.0)
    let calculateUserSimilarity (user1Connections: Guid list) (user2Connections: Guid list) : float =
        if List.isEmpty user1Connections && List.isEmpty user2Connections then 1.0
        else
            let intersection = 
                user1Connections
                |> List.filter (fun c -> List.contains c user2Connections)
                |> List.length

            let union = 
                (user1Connections @ user2Connections)
                |> List.distinct
                |> List.length

            float intersection / float union

    /// Viral potential score
    let calculateViralPotential (likes: int) (shares: int) (comments: int) (followers: int) : float =
        let engagementRate = float (likes + shares + comments) / float (max 1 followers)
        let shareWeight = float shares * 2.0 // Shares are more valuable
        let commentWeight = float comments * 1.5
        let likeWeight = float likes * 1.0

        (shareWeight + commentWeight + likeWeight) * engagementRate
