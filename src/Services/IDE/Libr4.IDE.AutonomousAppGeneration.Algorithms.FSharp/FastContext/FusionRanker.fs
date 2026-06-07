module Libr4.IDE.AutonomousAppGeneration.Algorithms.FastContext.FusionRanker

open System
open System.Collections.Generic

type SearchHitDto =
    { Path: string
      StartLine: int
      Score: float
      MatchKind: string
      Snippet: string }

type FusionOptions =
    { RrfK: int
      RipgrepWeight: float
      GraphWeight: float
      PathHeuristicWeight: float }

type FusedHitDto =
    { Path: string
      StartLine: int
      Score: float
      MatchKind: string
      Snippet: string }

let private pathBoost (path: string) =
    let p = path.Replace('\\', '/').ToLowerInvariant()
    if p.Contains("/models/") || p.EndsWith("models.py") then 1.0
    elif p.Contains("/services/") || p.Contains("/controllers/") then 0.7
    elif p.Contains("/components/") then 0.6
    else 0.0

let private key (hit: SearchHitDto) = $"{hit.Path}:{hit.StartLine}"

type FusionAccumulator(hit: SearchHitDto) =
    member val Hit = hit with get, set
    member val Score = 0.0 with get, set
    member val Kinds = HashSet<string>(StringComparer.OrdinalIgnoreCase) with get, set

let fuse
    (ripgrepHits: SearchHitDto[])
    (graphBoosts: (SearchHitDto * float)[])
    (limit: int)
    (options: FusionOptions)
    =
    let k = max 1 options.RrfK
    let scores = Dictionary<string, FusionAccumulator>(StringComparer.OrdinalIgnoreCase)

    let getAcc (hit: SearchHitDto) =
        let k' = key hit
        match scores.TryGetValue k' with
        | true, acc -> acc
        | false, _ ->
            let acc = FusionAccumulator(hit)
            scores.[k'] <- acc
            acc

    for i in 0 .. ripgrepHits.Length - 1 do
        let hit = ripgrepHits.[i]
        let acc = getAcc hit
        acc.Score <- acc.Score + options.RipgrepWeight * (1.0 / float (k + i + 1))
        acc.Kinds.Add("ripgrep") |> ignore

    graphBoosts
    |> Array.sortByDescending (fun (_, boost) -> boost)
    |> Array.iteri (fun i (hit, boost) ->
        if boost > 0.0 then
            let acc = getAcc hit
            acc.Score <- acc.Score + options.GraphWeight * boost * (1.0 / float (k + i + 1))
            acc.Kinds.Add("graph") |> ignore)

    ripgrepHits
    |> Array.mapi (fun idx hit -> hit, idx, pathBoost hit.Path)
    |> Array.filter (fun (_, _, boost) -> boost > 0.0)
    |> Array.sortByDescending (fun (_, _, boost) -> boost)
    |> Array.iteri (fun i (hit, _, boost) ->
        let acc = getAcc hit
        acc.Score <- acc.Score + options.PathHeuristicWeight * boost * (1.0 / float (k + i + 1))
        acc.Kinds.Add("path") |> ignore)

    scores.Values
    |> Seq.sortBy (fun acc -> -acc.Score, acc.Hit.Path)
    |> Seq.truncate limit
    |> Seq.map (fun acc ->
        let kind =
            if acc.Kinds.Count > 1 then "fusion"
            elif acc.Kinds.Count = 1 then acc.Kinds |> Seq.head
            else acc.Hit.MatchKind
        { Path = acc.Hit.Path
          StartLine = acc.Hit.StartLine
          Score = acc.Score
          MatchKind = kind
          Snippet = acc.Hit.Snippet })
    |> Array.ofSeq
