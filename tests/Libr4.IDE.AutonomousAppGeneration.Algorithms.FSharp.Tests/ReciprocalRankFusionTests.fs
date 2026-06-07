module ReciprocalRankFusionTests

open Xunit
open Libr4.IDE.AutonomousAppGeneration.Algorithms.Memory.ReciprocalRankFusion

[<Fact>]
let ``fuse prefers items present in multiple lists`` () =
    let fused =
        fuse
            [|
                [| "alpha"; "beta"; "gamma" |]
                [| "beta"; "alpha"; "delta" |]
            |]
            defaultK

    Assert.True(fused.Length >= 2)
    let topTwo = fused.[0..1] |> Array.map (fun x -> x.Id) |> Set.ofArray
    Assert.True(topTwo.Contains "beta")
    Assert.True(topTwo.Contains "alpha")
    Assert.True(fused.[0].Score >= fused.[2].Score)

[<Fact>]
let ``fuse returns empty for no lists`` () =
    let fused = fuse Array.empty defaultK
    Assert.Empty(fused)

[<Fact>]
let ``fuse score decreases with rank`` () =
    let fused = fuse [| [| "only" |] |] defaultK
    Assert.Equal(1, fused.Length)
    Assert.Equal(1.0 / (defaultK + 1.0), fused.[0].Score, 6)
