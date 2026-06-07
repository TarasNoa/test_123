module Libr4.IDE.AutonomousAppGeneration.Algorithms.MetaAgent.AgentSpecEvolution

open System
open System.Collections.Generic

type QualityGateDto = { Stage: string; Passed: bool; Reasons: string[] }

type FailedRunDto =
    { StatusFailed: bool
      VerifyGateFailed: bool
      VerifyGateReasons: string[]
      FailedIterations: int
      FailureReason: string option
      PipelineStage: string
      FileCount: int }

type ProposalDiffDto =
    { NewMaxTurns: int option
      ToolsToAdd: string[]
      InstructionAppend: string option }

type ProposalDto =
    { SpecName: string
      Diff: ProposalDiffDto
      Rationale: string }

type SpecDocumentDto =
    { Name: string
      Extend: string option
      Model: string option
      MaxTurns: int option
      MaxTokens: int option
      Toolset: string[]
      Instruction: string option
      Permissions: string option }

let private containsIgnoreCase (text: string) (value: string) =
    text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0

let analyze (run: FailedRunDto) =
    if not run.StatusFailed then
        Array.empty
    else
        let proposals = ResizeArray<ProposalDto>()

        if run.VerifyGateFailed then
            let reasons =
                run.VerifyGateReasons
                |> Array.truncate 2
                |> String.concat "; "

            proposals.Add(
                { SpecName = "verify"
                  Diff =
                    { NewMaxTurns = Some 16
                      ToolsToAdd = [| "browser_snapshot"; "browser_screenshot" |]
                      InstructionAppend =
                        Some
                            "When verify fails, capture browser evidence and cross-check readiness artifacts before concluding failure." }
                  Rationale = $"Verify gate failed: {reasons}" }
            )

        let failureReason = defaultArg run.FailureReason "n/a"

        if run.FailedIterations >= 2 || containsIgnoreCase failureReason "repair" then
            proposals.Add(
                { SpecName = "repair"
                  Diff =
                    { NewMaxTurns = Some 22
                      ToolsToAdd = [| "todo_write"; "run_tests" |]
                      InstructionAppend =
                        Some
                            "Prioritize minimal patches validated by run_build/run_tests; track repair attempts with todo_write." }
                  Rationale = $"Repair loop stress: failedIterations={run.FailedIterations}, reason={failureReason}" }
            )

        let stage = run.PipelineStage

        if
            containsIgnoreCase stage "Generation"
            || containsIgnoreCase stage "Plan"
            || run.FileCount = 0
        then
            proposals.Add(
                { SpecName = "implementer"
                  Diff =
                    { NewMaxTurns = Some 20
                      ToolsToAdd = [| "list_directory"; "glob" |]
                      InstructionAppend =
                        Some
                            "Validate scaffold completeness early: ensure build/test entrypoints exist before deep feature work." }
                  Rationale = $"Early-stage failure at pipeline stage `{stage}` with {run.FileCount} files." }
            )

        proposals
        |> Seq.groupBy (fun p -> p.SpecName.ToLowerInvariant())
        |> Seq.map (fun (_, group) -> group |> Seq.head)
        |> Array.ofSeq

let applyDiff (baseline: SpecDocumentDto) (diff: ProposalDiffDto) =
    let maxTurns =
        match diff.NewMaxTurns with
        | Some v when v > 0 -> Some(max (defaultArg baseline.MaxTurns 12) v)
        | _ -> baseline.MaxTurns

    let toolset = ResizeArray<string>(baseline.Toolset)

    for tool in diff.ToolsToAdd do
        if not (toolset |> Seq.exists (fun t -> t.Equals(tool, StringComparison.OrdinalIgnoreCase))) then
            toolset.Add tool

    let instruction =
        match diff.InstructionAppend with
        | None | Some "" -> baseline.Instruction
        | Some append ->
            match baseline.Instruction with
            | None | Some "" -> Some (append.Trim())
            | Some existing -> Some(existing.TrimEnd() + "\n\n" + append.Trim())

    { baseline with
        MaxTurns = maxTurns
        Toolset = toolset.ToArray()
        Instruction = instruction }

let buildDiffPreview (before: SpecDocumentDto) (after: SpecDocumentDto) =
    let lines = ResizeArray<string>()

    if before.MaxTurns <> after.MaxTurns then
        lines.Add($"maxTurns: {before.MaxTurns} -> {after.MaxTurns}")

    let beforeTools = HashSet<string>(before.Toolset, StringComparer.OrdinalIgnoreCase)

    for tool in after.Toolset do
        if not (beforeTools.Contains tool) then
            lines.Add($"+ toolset: {tool}")

    let beforeInstruction = defaultArg before.Instruction ""
    let afterInstruction = defaultArg after.Instruction ""

    if not (String.Equals(beforeInstruction, afterInstruction, StringComparison.Ordinal)) then
        lines.Add("+ instruction: appended KLIP evolution guidance")

    if lines.Count = 0 then "(no diff)" else String.Join('\n', lines)
