module Libr4.IDE.AutonomousAppGeneration.Algorithms.Context.ContextFragmentBudget

open System
open System.Collections.Generic

type FragmentDto =
    { TypeKey: string
      TypeOrdinal: int
      Content: string
      Priority: int
      Provenance: (string * string)[] }

let private truncate (text: string) (maxLen: int) =
    if text.Length <= maxLen then text else text.[0 .. maxLen - 1] + "…"

let defaultPriority (typeOrdinal: int) =
    match typeOrdinal with
    | 1 -> 100 // ErrorReport
    | 0 -> 90 // BuildLog
    | 2 -> 80 // FileExcerpt
    | 4 -> 70 // VerifyEvidence
    | 5 -> 85 // LspDiagnostics
    | 6 -> 82 // GitDiff
    | 7 -> 78 // FastContext
    | 3 -> 60 // DesignArtifact
    | _ -> 50

let formatMarker (typeKey: string) (provenance: (string * string)[]) =
    let parts = ResizeArray<string>()
    parts.Add("fragment")
    parts.Add(typeKey)

    provenance
    |> Array.sortBy (fun (k, _) -> k)
    |> Array.iter (fun (k, v) -> parts.Add(k + "=" + v))

    "[" + String.Join(":", parts) + "]"

let private markerLength (typeKey: string) (provenance: (string * string)[]) =
    formatMarker typeKey provenance |> String.length

let private charsFor (fragments: FragmentDto[]) =
    fragments
    |> Array.sumBy (fun f ->
        markerLength f.TypeKey f.Provenance + f.Content.Length + 2)

let private getCap (caps: IReadOnlyDictionary<string, int>) (typeKey: string) =
    match caps.TryGetValue typeKey with
    | true, cap -> cap
    | false, _ -> 4000

let normalizeFragment (fragment: FragmentDto) (caps: IReadOnlyDictionary<string, int>) =
    let cap = getCap caps fragment.TypeKey
    let priority =
        if fragment.Priority > 0 then fragment.Priority
        else defaultPriority fragment.TypeOrdinal

    { fragment with
        Content = truncate fragment.Content cap
        Priority = priority }

let selectWithinBudget (fragments: FragmentDto[]) (maxTotalChars: int) (caps: IReadOnlyDictionary<string, int>) =
    let ordered =
        fragments
        |> Array.map (fun f -> normalizeFragment f caps)
        |> Array.sortByDescending (fun f -> f.Priority, -f.TypeOrdinal)
        |> Array.toList

    let rec dropLast (items: FragmentDto list) =
        if items.Length = 0 then items
        elif charsFor (List.toArray items) <= maxTotalChars then items
        else dropLast (items.[0 .. items.Length - 2])

    let mutable selected = dropLast ordered |> List.toArray

    if selected.Length = 0 then
        selected
    else
        while charsFor selected > maxTotalChars do
            let last = selected.[selected.Length - 1]
            let over = charsFor selected - maxTotalChars
            let newLen = max 120 (last.Content.Length - over - 1)
            let trimmed =
                if newLen >= last.Content.Length then last.Content
                else last.Content.[0 .. newLen - 1] + "…"

            selected.[selected.Length - 1] <- { last with Content = trimmed }

        selected

let assemble (fragments: FragmentDto[]) (maxTotalChars: int) (caps: IReadOnlyDictionary<string, int>) =
    let selected = selectWithinBudget fragments maxTotalChars caps

    selected
    |> Array.map (fun f ->
        $"{formatMarker f.TypeKey f.Provenance}{Environment.NewLine}{f.Content}")
    |> String.concat (Environment.NewLine + Environment.NewLine)
    |> fun text -> text.TrimEnd()
