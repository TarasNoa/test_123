module Wave2ModuleTests

open System
open System.Collections.Generic
open Xunit
open Libr4.IDE.AutonomousAppGeneration.Algorithms.Memory.HermesMemoryScoring
open Libr4.IDE.AutonomousAppGeneration.Algorithms.Context.HeuristicSemanticCompactor
open Libr4.IDE.AutonomousAppGeneration.Algorithms.Context.ContextFragmentBudget
open Libr4.IDE.AutonomousAppGeneration.Algorithms.ModelRouting.RoleModelCircuit
open Libr4.IDE.AutonomousAppGeneration.Algorithms.MetaAgent.AgentSpecEvolution
open Libr4.IDE.AutonomousAppGeneration.Algorithms.AgentRuntime.AgentResponseParser

[<Fact>]
let ``HermesMemoryScoring prefers procedural over episodic`` () =
    let now = DateTime.UtcNow
    let episodic =
        { Kind = 0
          Stage = "build"
          Key = "k1"
          Summary = "log"
          Score = 1.0
          CreatedAtUtc = now }

    let procedural =
        { episodic with
            Kind = int MemoryKindDto.Procedural
            Key = "k2" }

    let eScore = computeRelevanceScore episodic None now
    let pScore = computeRelevanceScore procedural None now
    Assert.True(pScore > eScore)

[<Fact>]
let ``HermesMemoryScoring keyword match increases score`` () =
    let now = DateTime.UtcNow

    let entry =
        { Kind = 0
          Stage = "verify"
          Key = "auth"
          Summary = "token refresh failed"
          Score = 0.0
          CreatedAtUtc = now }

    let without = computeRelevanceScore entry None now
    let withKw = computeRelevanceScore entry (Some "token") now
    Assert.True(withKw > without)
    Assert.Contains("keyword_match", buildRetrievalReason entry (Some "token"))

[<Fact>]
let ``HeuristicSemanticCompactor extracts file paths and errors`` () =
    let turns =
        [| { Role = "assistant"; Content = "Patch src/app/main.ts and backend/api/routes.ts" }
           { Role = "tool"; Content = "error: ModuleNotFound in src/app/main.ts" } |]

    let summary = summarize turns Array.empty
    Assert.Contains("src/app/main.ts", summary.FilesTouched)
    Assert.True(summary.OpenIssues.Length >= 1)

[<Fact>]
let ``ContextFragmentBudget respects total char budget`` () =
    let caps = Dictionary<string, int>() :> IReadOnlyDictionary<string, int>

    let fragments =
        [| { TypeKey = "BuildLog"
             TypeOrdinal = 0
             Content = String('x', 5000)
             Priority = 0
             Provenance = [| "run", "1" |] }
           { TypeKey = "ErrorReport"
             TypeOrdinal = 1
             Content = String('y', 5000)
             Priority = 0
             Provenance = [| "run", "1" |] } |]

    let assembled = assemble fragments 2000 caps
    Assert.True(assembled.Length <= 2000)

[<Fact>]
let ``RoleModelCircuit opens after threshold failures`` () =
    let now = DateTime.UtcNow
    let mutable state = createClosed ()
    state <- onFailure state 1 now
    state <- onFailure state 1 now
    Assert.Equal(openState, state.Current)
    Assert.True(isOpen state now 30)

[<Fact>]
let ``RoleModelCircuit buildKey normalizes role`` () =
    Assert.Equal("implementer:gpt-4", buildKey "CUSTOM" "gpt-4")
    Assert.Equal("verify:claude", buildKey "verify" "claude")

[<Fact>]
let ``AgentSpecEvolution proposes verify tools on gate failure`` () =
    let run =
        { StatusFailed = true
          VerifyGateFailed = true
          VerifyGateReasons = [| "smoke test timeout" |]
          FailedIterations = 2
          FailureReason = None
          PipelineStage = "verify"
          FileCount = 3 }

    let proposals = analyze run
    Assert.NotEmpty(proposals)
    Assert.Contains(proposals, fun p -> p.SpecName = "verify" && p.Diff.ToolsToAdd.Length > 0)

[<Fact>]
let ``AgentResponseParser parses tool action`` () =
    let raw = """{"action":"tool","tool":"search_codebase","input":{"query":"auth"}}"""
    let parsed = parse raw true
    Assert.Equal(0, parsed.Action)
    Assert.Equal(Some "search_codebase", parsed.ToolName)

[<Fact>]
let ``AgentResponseParser parses done action`` () =
    let raw = """{"action":"done","summary":"completed"}"""
    let parsed = parse raw false
    Assert.Equal(1, parsed.Action)
    Assert.Equal(Some "completed", parsed.Summary)
