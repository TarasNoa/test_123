module Libr4.IDE.AutonomousAppGeneration.Algorithms.Patching.UnifiedDiffParser

open System

type DiffHunkDto =
    { OldStart: int
      OldCount: int
      NewStart: int
      NewCount: int
      Lines: string[] }

type UnifiedDiffDto = { TargetPath: string option; Hunks: DiffHunkDto[] }

let private parseRange (token: string) =
    let pieces = token.Split(',')
    let mutable startVal = 0
    if pieces.Length > 0 then
        Int32.TryParse(pieces.[0], &startVal) |> ignore
    let mutable countVal = 1
    if pieces.Length > 1 then
        Int32.TryParse(pieces.[1], &countVal) |> ignore
    startVal, countVal

let parse (patch: string) (fallbackPath: string option) =
    let lines = patch.Replace("\r\n", "\n").Split('\n')
    let mutable path = fallbackPath
    let hunks = ResizeArray<DiffHunkDto>()
    let mutable current: ResizeArray<string> option = None
    let mutable oldStart = 0
    let mutable oldCount = 0
    let mutable newStart = 0
    let mutable newCount = 0

    let flush () =
        match current with
        | None -> ()
        | Some linesAcc ->
            hunks.Add(
                { OldStart = oldStart
                  OldCount = oldCount
                  NewStart = newStart
                  NewCount = newCount
                  Lines = linesAcc.ToArray() })
            current <- None

    for line in lines do
        if line.StartsWith("+++ ", StringComparison.Ordinal) then
            if path.IsNone then
                let raw = line.Substring(4).Trim()
                path <- Some(if raw.StartsWith('b') then raw.Substring(1).Trim() else raw)
        elif line.StartsWith("@@", StringComparison.Ordinal) then
            flush ()
            current <- Some(ResizeArray<string>())
            let parts = line.Split(' ')
            if parts.Length >= 3 then
                let oStart, oCount = parseRange(parts.[1].TrimStart('-'))
                let nStart, nCount = parseRange(parts.[2].TrimStart('+'))
                oldStart <- oStart
                oldCount <- oCount
                newStart <- nStart
                newCount <- nCount
        elif current.IsSome && line.Length > 0 then
            match line.[0] with
            | ' ' | '+' | '-' -> current.Value.Add(line)
            | _ -> ()

    flush ()
    { TargetPath = path; Hunks = hunks.ToArray() }
