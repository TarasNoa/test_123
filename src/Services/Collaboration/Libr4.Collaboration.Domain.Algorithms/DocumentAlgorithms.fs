namespace Libr4.Collaboration.Domain.Algorithms

open System

[<CLIMutable>]
type DocumentData =
    { Id: Guid
      RoomId: Guid
      Name: string
      Type: string
      Content: string
      Versions: DocumentVersion list
      LastModifiedAt: DateTimeOffset
      LastModifiedBy: Guid }

and [<CLIMutable>] DocumentVersion =
    { Version: int
      Content: string
      AuthorId: Guid
      CreatedAt: DateTimeOffset }

module DocumentAlgorithms =

    let calculateDiff (oldContent: string) (newContent: string) =
        let oldLines = oldContent.Split([|'\n'|])
        let newLines = newContent.Split([|'\n'|])
        let insertions = newLines.Length - oldLines.Length
        let deletions = if insertions < 0 then -insertions else 0
        { Insertions = max insertions 0; Deletions = deletions; LinesChanged = abs insertions }

    let detectConflicts (version1: DocumentVersion) (version2: DocumentVersion) =
        version1.AuthorId <> version2.AuthorId && version1.CreatedAt.AddSeconds(10.0) > version2.CreatedAt

    let mergeVersions (base_: DocumentVersion) (version1: DocumentVersion) (version2: DocumentVersion) =
        if detectConflicts version1 version2 then
            {| Conflict = true; ResolvedContent = "" |}
        else
            {| Conflict = false; ResolvedContent = version2.Content |}

    let trackChanges (oldContent: string) (newContent: string) (userId: Guid) =
        let diff = calculateDiff oldContent newContent
        { UserId = userId; Timestamp = DateTimeOffset.UtcNow; Changes = diff }

    let extractCodeBlocks (content: string) =
        let pattern = @"```(\w+)\n([\s\S]*?)```"
        System.Text.RegularExpressions.Regex.Matches(content, pattern)
        |> Seq.map (fun m -> { Language = m.Groups.[1].Value; Code = m.Groups.[2].Value })
        |> Seq.toList

    let applyOperationalTransform (document: DocumentData) (operation: OTOperation) =
        match operation with
        | Insert (pos, text) -> 
            let before = document.Content.Substring(0, pos)
            let after = document.Content.Substring(pos)
            { document with Content = before + text + after }
        | Delete (pos, length) ->
            let before = document.Content.Substring(0, pos)
            let after = document.Content.Substring(pos + length)
            { document with Content = before + after }
        | Replace (pos, oldText, newText) ->
            let before = document.Content.Substring(0, pos)
            let after = document.Content.Substring(pos + oldText.Length)
            { document with Content = before + newText + after }

type DiffResult = { Insertions: int; Deletions: int; LinesChanged: int }
type ChangeTracker = { UserId: Guid; Timestamp: DateTimeOffset; Changes: DiffResult }
type CodeBlock = { Language: string; Code: string }
type OTOperation = 
    | Insert of pos: int * text: string
    | Delete of pos: int * length: int
    | Replace of pos: int * oldText: string * newText: string