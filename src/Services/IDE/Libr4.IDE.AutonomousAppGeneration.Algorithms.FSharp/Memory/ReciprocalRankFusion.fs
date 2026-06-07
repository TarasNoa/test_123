module Libr4.IDE.AutonomousAppGeneration.Algorithms.Memory.ReciprocalRankFusion

open System
open System.Collections.Generic

let defaultK = 60.0

type FusedRankDto = { Id: string; Score: float }

let fuse (rankedLists: string[][]) (k: float) =
    if rankedLists.Length = 0 then
        Array.empty
    else
        let scores = Dictionary<string, float>(StringComparer.Ordinal)

        for list in rankedLists do
            for rank in 0 .. list.Length - 1 do
                let id = list.[rank]
                let current =
                    match scores.TryGetValue id with
                    | true, v -> v
                    | false, _ -> 0.0

                scores.[id] <- current + 1.0 / (k + float rank + 1.0)

        scores
        |> Seq.sortByDescending (fun kv -> kv.Value)
        |> Seq.map (fun kv -> { Id = kv.Key; Score = kv.Value })
        |> Array.ofSeq
