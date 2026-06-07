module TopologicalSorterTests

open Xunit
open Libr4.IDE.AutonomousAppGeneration.Algorithms.RepoGraph.TopologicalSorter

[<Fact>]
let ``sortGenerationOrder places dependency before dependent`` () =
    let paths = [| "src/a.ts"; "src/b.ts"; "src/c.ts" |]
    let edges = [| ("src/a.ts", "src/b.ts", "import"); ("src/b.ts", "src/c.ts", "import") |]
    let ordered = sortGenerationOrder paths edges

    let idx path = Array.findIndex ((=) path) ordered
    Assert.True(idx "src/c.ts" < idx "src/b.ts")
    Assert.True(idx "src/b.ts" < idx "src/a.ts")

[<Fact>]
let ``sortRepairOrder reverses generation order`` () =
    let generation = [| "c.ts"; "b.ts"; "a.ts" |]
    let repair = sortRepairOrder generation
    Assert.Equal<string array>([| "a.ts"; "b.ts"; "c.ts" |], repair)

[<Fact>]
let ``sortGenerationOrder breaks cycles deterministically`` () =
    let paths = [| "a.ts"; "b.ts" |]
    let edges = [| ("a.ts", "b.ts", "import"); ("b.ts", "a.ts", "import") |]
    let ordered = sortGenerationOrder paths edges
    Assert.Equal(2, ordered.Length)
    Assert.Contains("a.ts", ordered)
    Assert.Contains("b.ts", ordered)
