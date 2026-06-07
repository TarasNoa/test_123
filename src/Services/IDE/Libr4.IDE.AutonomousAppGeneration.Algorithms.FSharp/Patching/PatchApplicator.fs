module Libr4.IDE.AutonomousAppGeneration.Algorithms.Patching.PatchApplicator

open System
open UnifiedDiffParser

type PatchApplyMode =
    | Exact = 0
    | Fuzzy = 1
    | ThreeWay = 2

type PatchApplyResultDto =
    { Success: bool
      PatchedContent: string option
      ConflictReport: string option
      Mode: PatchApplyMode }

let private normalize (content: string) = content.Replace("\r\n", "\n")

let private buildSearchBlock (hunk: DiffHunkDto) =
    (hunk.Lines
     |> Array.filter (fun l -> l.StartsWith(' ') || l.StartsWith('-'))
     |> Array.map (fun l -> l.Substring(1))
     |> String.concat "\n")
    + "\n"

let private buildReplaceBlock (hunk: DiffHunkDto) =
    (hunk.Lines
     |> Array.filter (fun l -> l.StartsWith(' ') || l.StartsWith('+'))
     |> Array.map (fun l -> l.Substring(1))
     |> String.concat "\n")
    + "\n"

let private findFuzzy (content: string) (search: string) =
    let trimmed = search.TrimEnd('\n')
    let lines = trimmed.Split('\n', StringSplitOptions.RemoveEmptyEntries)
    if lines.Length = 0 then -1
    else content.IndexOf(lines.[0], StringComparison.Ordinal)

let private applyHunksExact (original: string) (hunks: DiffHunkDto[]) =
    let mutable content = normalize original
    let mutable error: string option = None

    for hunk in hunks do
        if error.IsNone then
            let search = buildSearchBlock hunk
            if not (content.Contains(search, StringComparison.Ordinal)) then
                error <- Some $"exact hunk not found at oldStart={hunk.OldStart}"
            else
                let replace = buildReplaceBlock hunk
                content <- content.Replace(search, replace, StringComparison.Ordinal)

    match error with
    | Some reason -> Error reason
    | None -> Ok content

let private applyHunksFuzzy (original: string) (hunks: DiffHunkDto[]) =
    match applyHunksExact original hunks with
    | Ok content -> Ok content
    | Error _ ->
        let mutable content = normalize original
        let mutable error: string option = None

        for hunk in hunks do
            if error.IsNone then
                let search = (buildSearchBlock hunk).TrimEnd('\n')
                let mutable idx = content.IndexOf(search, StringComparison.Ordinal)
                if idx < 0 then
                    idx <- findFuzzy content search
                if idx < 0 then
                    error <- Some "fuzzy match failed"
                elif idx + search.Length > content.Length then
                    error <- Some "fuzzy match out of bounds"
                else
                    let replace = (buildReplaceBlock hunk).TrimEnd('\n')
                    content <- content.Substring(0, idx) + replace + content.Substring(idx + search.Length)

        match error with
        | Some reason -> Error reason
        | None -> Ok content

let applyExact (original: string) (diff: UnifiedDiffDto) =
    if diff.Hunks.Length = 0 then
        { Success = false
          PatchedContent = None
          ConflictReport = Some "no hunks"
          Mode = PatchApplyMode.Exact }
    else
        match applyHunksExact original diff.Hunks with
        | Ok patched ->
            { Success = true
              PatchedContent = Some patched
              ConflictReport = None
              Mode = PatchApplyMode.Exact }
        | Error reason ->
            { Success = false
              PatchedContent = None
              ConflictReport = Some reason
              Mode = PatchApplyMode.Exact }

let applyFuzzy (original: string) (diff: UnifiedDiffDto) =
    let exact = applyExact original diff
    if exact.Success then exact
    else
        match applyHunksFuzzy original diff.Hunks with
        | Ok patched ->
            { Success = true
              PatchedContent = Some patched
              ConflictReport = None
              Mode = PatchApplyMode.Fuzzy }
        | Error reason ->
            { Success = false
              PatchedContent = None
              ConflictReport = Some reason
              Mode = PatchApplyMode.Fuzzy }

let applyThreeWay (original: string) (baseContent: string option) (diff: UnifiedDiffDto) =
    match baseContent with
    | None | Some "" -> applyFuzzy original diff
    | Some baseText ->
        let baseNorm = normalize baseText
        let oursNorm = normalize original

        match applyExact baseNorm diff with
        | { Success = true; PatchedContent = Some theirs } ->
            if oursNorm = baseNorm || oursNorm = theirs then
                { Success = true
                  PatchedContent = Some theirs
                  ConflictReport = None
                  Mode = PatchApplyMode.ThreeWay }
            else
                match applyFuzzy oursNorm diff with
                | { Success = true; PatchedContent = patched } ->
                    { Success = true
                      PatchedContent = patched
                      ConflictReport = None
                      Mode = PatchApplyMode.ThreeWay }
                | _ ->
                    { Success = false
                      PatchedContent = None
                      ConflictReport =
                          Some(
                              "three-way merge conflict: local changes overlap patch; resolve manually"
                          )
                      Mode = PatchApplyMode.ThreeWay }
        | _ ->
            match applyFuzzy oursNorm diff with
            | { Success = true } as ok -> { ok with Mode = PatchApplyMode.ThreeWay }
            | fail ->
                { fail with
                    ConflictReport =
                        Some(
                            match fail.ConflictReport with
                            | Some r -> $"three-way merge conflict: {r}"
                            | None -> "three-way merge conflict"
                        )
                    Mode = PatchApplyMode.ThreeWay }
