module Libr4.IDE.AutonomousAppGeneration.Algorithms.AgentRuntime.AgentSessionTurnMachine

open System

type PatchEntryDto =
    { Path: string
      Content: string }

type TurnCountersDto =
    { ConsecutiveInvalidTurns: int
      ConsecutiveReadOnlyTools: int }

type TurnAction =
    | Done = 0
    | Tool = 1
    | Invalid = 2

type AfterParseDecision =
    | AcceptDone = 0
    | RejectDoneMissingTargets = 1
    | ExecuteTool = 2
    | InvalidTurn = 3
    | InvalidTurnWithNudge = 4

type AfterToolDecision =
    | Continue = 0
    | InvestigationNudge = 1

let targetsSatisfied (patches: PatchEntryDto[]) (targetPaths: string[]) (minChars: int) =
    if targetPaths.Length = 0 then patches.Length > 0
    else
        targetPaths
        |> Array.forall (fun path ->
            patches
            |> Array.exists (fun p ->
                String.Equals(p.Path, path, StringComparison.OrdinalIgnoreCase)
                && not (String.IsNullOrWhiteSpace p.Content)
                && p.Content.Length >= minChars))

let filterPatches (patches: PatchEntryDto[]) (targetPaths: string[]) =
    if targetPaths.Length = 0 then patches
    else
        let allowed =
            targetPaths
            |> Array.filter (fun p -> not (String.IsNullOrWhiteSpace p))
            |> Array.map (fun p -> p.ToLowerInvariant())
            |> Set.ofArray
        patches |> Array.filter (fun p -> allowed.Contains(p.Path.ToLowerInvariant()))

let decideAfterParse (isGeneration: bool) (action: TurnAction) (hasToolCall: bool) (patches: PatchEntryDto[]) (targetPaths: string[]) (minChars: int) =
    match action with
    | TurnAction.Done ->
        if isGeneration && not (targetsSatisfied patches targetPaths minChars) then AfterParseDecision.RejectDoneMissingTargets
        else AfterParseDecision.AcceptDone
    | TurnAction.Tool when hasToolCall -> AfterParseDecision.ExecuteTool
    | _ -> AfterParseDecision.InvalidTurn

let decideAfterInvalidTurn (recoveryHasTool: bool) (recoveryNudge: string option) =
    if recoveryHasTool then AfterParseDecision.InvalidTurn
    else
        match recoveryNudge with
        | None | Some "" -> AfterParseDecision.InvalidTurn
        | Some _ -> AfterParseDecision.InvalidTurnWithNudge

let decideAfterTool (isGeneration: bool) (counters: TurnCountersDto) (maxInvestigationReadOnly: int) (patches: PatchEntryDto[]) (targetPaths: string[]) (minChars: int) (toolIsReadOnly: bool) =
    let readOnlyCount = if toolIsReadOnly then counters.ConsecutiveReadOnlyTools + 1 else 0
    if isGeneration && readOnlyCount >= maxInvestigationReadOnly && not (targetsSatisfied patches targetPaths minChars) then
        AfterToolDecision.InvestigationNudge, { counters with ConsecutiveReadOnlyTools = 0 }
    else
        AfterToolDecision.Continue, { counters with ConsecutiveReadOnlyTools = readOnlyCount }

let incrementInvalidTurn (counters: TurnCountersDto) =
    { counters with ConsecutiveInvalidTurns = counters.ConsecutiveInvalidTurns + 1 }

let resetInvalidTurn (counters: TurnCountersDto) =
    { counters with ConsecutiveInvalidTurns = 0 }
