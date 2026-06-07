module ReciprocalRankFusionPropertyTests

open FsCheck
open FsCheck.Xunit
open Libr4.IDE.AutonomousAppGeneration.Algorithms.Memory.ReciprocalRankFusion

[<Property(MaxTest = 60)>]
let ``fuse preserves all unique ids from input lists`` (lists: list<list<NonEmptyString>>) =
    let ranked =
        lists
        |> List.map (List.map (fun s -> s.Get) >> List.toArray)
        |> List.toArray

    if ranked.Length = 0 then
        true
    else
        let fused = fuse ranked defaultK
        let expected =
            ranked
            |> Array.collect id
            |> Array.distinct
            |> Set.ofArray

        let actual = fused |> Array.map (fun x -> x.Id) |> Set.ofArray
        expected = actual

[<Property(MaxTest = 60)>]
let ``fuse scores are non-negative`` (lists: list<list<NonEmptyString>>) =
    let ranked =
        lists
        |> List.map (List.map (fun s -> s.Get) >> List.toArray)
        |> List.toArray

    let fused = fuse ranked defaultK
    fused |> Array.forall (fun x -> x.Score >= 0.0)
