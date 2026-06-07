module RepairPlaybookSignatureTests

open Xunit
open Libr4.IDE.AutonomousAppGeneration.Algorithms.Playbook.RepairPlaybookSignature

[<Fact>]
let ``fromError is stable for same inputs`` () =
    let a = fromError "compile" (Some "src/App.tsx") "Type 'string' is not assignable to type 'number'"
    let b = fromError "compile" (Some "src/App.tsx") "Type 'string' is not assignable to type 'number'"
    Assert.Equal(a, b)

[<Fact>]
let ``fromError changes when message changes`` () =
    let a = fromError "compile" (Some "src/App.tsx") "missing semicolon"
    let b = fromError "compile" (Some "src/App.tsx") "undefined variable foo"
    Assert.True(a <> b)

[<Fact>]
let ``buildStackPattern includes application and stack`` () =
    let pattern = buildStackPattern (Some "MyApp") [| "typescript" |] [| "react" |]
    Assert.Contains("myapp", pattern)
    Assert.Contains("typescript", pattern)
    Assert.Contains("react", pattern)

[<Fact>]
let ``fromErrors returns signature and stack pattern`` () =
    let errors = [| ("compile", Some "Program.cs", "CS1002 ; expected") |]
    let signature, stack =
        fromErrors errors None (Some "Demo") [| "csharp" |] [| "aspnetcore" |]

    Assert.False(System.String.IsNullOrWhiteSpace signature)
    Assert.Contains("demo", stack)
