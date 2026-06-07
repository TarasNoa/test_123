module RepoGraphEngineTests

open System.Collections.Generic
open Xunit
open Libr4.IDE.AutonomousAppGeneration.Algorithms.RepoGraph.RepoGraphEngine

let private contents (pairs: (string * string) list) =
    let dict = Dictionary<string, string>()
    for key, value in pairs do
        dict.[key] <- value
    dict :> IReadOnlyDictionary<string, string>

[<Fact>]
let ``buildGraph links typescript import edges`` () =
    let paths = [| "src/App.tsx"; "src/components/Button.tsx" |]
    let graph =
        buildGraph
            paths
            (contents
                [ "src/App.tsx", "import { Button } from './components/Button';\n"
                  "src/components/Button.tsx", "export const Button = () => null;\n" ])

    Assert.Equal(2, graph.Files.Length)
    Assert.Contains(graph.Edges, fun e -> e.FromPath = "src/App.tsx" && e.ToPath = "src/components/Button.tsx")

[<Fact>]
let ``orderForGeneration places imported file before importer`` () =
    let paths = [| "src/App.tsx"; "src/components/Button.tsx" |]
    let ordered =
        orderForGeneration
            paths
            (contents
                [ "src/App.tsx", "import { Button } from './components/Button';\n"
                  "src/components/Button.tsx", "export const Button = () => null;\n" ])

    let buttonIdx = Array.findIndex ((=) "src/components/Button.tsx") ordered
    let appIdx = Array.findIndex ((=) "src/App.tsx") ordered
    Assert.True(buttonIdx < appIdx)

[<Fact>]
let ``orderForRepair reverses generation order`` () =
    let paths = [| "a.ts"; "b.ts" |]
    let generation = orderForGeneration paths (contents [ "a.ts", "import x from './b';\n"; "b.ts", "export default 1;\n" ])
    let repair = orderForRepair paths (contents [ "a.ts", "import x from './b';\n"; "b.ts", "export default 1;\n" ])
    Assert.Equal<string array>(Array.rev generation, repair)

[<Fact>]
let ``buildGraph adds django heuristic edges`` () =
    let paths = [| "api/views.py"; "api/models.py"; "api/serializers.py" |]
    let graph = buildGraph paths (contents [ for p in paths -> p, "# stub\n" ])
    Assert.Contains(graph.Edges, fun e -> e.Kind = "heuristic" && e.FromPath = "api/views.py" && e.ToPath = "api/models.py")
