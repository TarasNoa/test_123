module Libr4.IDE.AutonomousAppGeneration.Algorithms.Context.HeuristicSemanticCompactor

open System
open System.Collections.Generic
open System.Text.RegularExpressions

type ConversationTurnDto = { Role: string; Content: string }

type CompactionSummaryDto =
    { Decisions: string[]
      FilesTouched: string[]
      OpenIssues: string[]
      NextActions: string[]
      ErrorsResolved: string[] }

let private pathRegex =
    Regex(
        @"(?:backend|frontend|src|lib|app)/[\w./\-]+",
        RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

let private truncate (text: string) (maxLen: int) =
    if text.Length <= maxLen then text else text.[0 .. maxLen - 1] + "…"

let private isErrorLike (content: string) =
    content.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0
    || content.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0
    || content.IndexOf("rejected", StringComparison.OrdinalIgnoreCase) >= 0
    || content.IndexOf("ModuleNotFound", StringComparison.OrdinalIgnoreCase) >= 0

let private containsIgnoreCase (text: string) (value: string) =
    text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0

let summarize (turns: ConversationTurnDto[]) (manifestPaths: string[]) =
    let decisions = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    let files = HashSet<string>(manifestPaths, StringComparer.OrdinalIgnoreCase)
    let openIssues = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    let nextActions = List<string>()
    let resolved = HashSet<string>(StringComparer.OrdinalIgnoreCase)

    for turn in turns do
        for m in pathRegex.Matches turn.Content do
            files.Add(m.Value.Replace('\\', '/')) |> ignore

        if turn.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) then
            if
                containsIgnoreCase turn.Content "\"action\":\"done\""
                || containsIgnoreCase turn.Content "action\":\"done\""
            then
                decisions.Add(truncate turn.Content 240) |> ignore

            if containsIgnoreCase turn.Content "\"action\":\"tool\"" then
                nextActions.Add(truncate turn.Content 200)

        if isErrorLike turn.Content then
            openIssues.Add(truncate turn.Content 220) |> ignore

        if
            turn.Role.Equals("tool", StringComparison.OrdinalIgnoreCase)
            && (containsIgnoreCase turn.Content "success=true"
                || containsIgnoreCase turn.Content "] ok"
                || containsIgnoreCase turn.Content "applied")
        then
            resolved.Add(truncate turn.Content 180) |> ignore

    if manifestPaths.Length > 0 then
        let manifestNote =
            manifestPaths
            |> Array.truncate 12
            |> String.concat ", "
        decisions.Add($"manifest_paths_preserved: {manifestNote}") |> ignore

    { Decisions = decisions |> Seq.truncate 12 |> Array.ofSeq
      FilesTouched = files |> Seq.truncate 24 |> Array.ofSeq
      OpenIssues = openIssues |> Seq.truncate 12 |> Array.ofSeq
      NextActions = nextActions |> Seq.truncate 6 |> Array.ofSeq
      ErrorsResolved = resolved |> Seq.truncate 8 |> Array.ofSeq }
