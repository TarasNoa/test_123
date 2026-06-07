module Libr4.IDE.AutonomousAppGeneration.Algorithms.Memory.HermesMemoryScoring

open System

/// Mirrors <see cref="Libr4.IDE.Domain.AutonomousAppGeneration.MemoryKind"/> numeric values.
type MemoryKindDto =
    | Episodic = 0
    | Semantic = 1
    | Procedural = 2
    | Strategic = 3
    | Meta = 4

type MemoryEntryDto =
    { Kind: int
      Stage: string
      Key: string
      Summary: string
      Score: float
      CreatedAtUtc: DateTime }

let private kindBaseScore (kind: int) =
    match enum<MemoryKindDto> kind with
    | MemoryKindDto.Procedural -> 3.0
    | MemoryKindDto.Semantic -> 2.5
    | MemoryKindDto.Strategic -> 2.2
    | MemoryKindDto.Meta -> 2.0
    | _ -> 1.0

let private kindLabel (kind: int) =
    match enum<MemoryKindDto> kind with
    | MemoryKindDto.Procedural -> "L1_procedural"
    | MemoryKindDto.Semantic -> "L2_semantic"
    | MemoryKindDto.Strategic -> "L3_strategic"
    | MemoryKindDto.Meta -> "L4_meta"
    | _ -> "L0_episodic"

let private containsIgnoreCase (text: string) (keyword: string) =
    text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0

let computeRelevanceScore (entry: MemoryEntryDto) (keyword: string option) (nowUtc: DateTime) =
    let mutable score = kindBaseScore entry.Kind
    score <- score + entry.Score * 0.25

    let ageHours = max 0.0 (nowUtc - entry.CreatedAtUtc).TotalHours
    score <- score + max 0.0 (1.0 - min (ageHours / 24.0) 1.0)

    match keyword with
    | None | Some "" -> ()
    | Some kw ->
        if containsIgnoreCase entry.Summary kw then score <- score + 2.0
        if containsIgnoreCase entry.Key kw then score <- score + 1.5
        if containsIgnoreCase entry.Stage kw then score <- score + 1.0

    score

let buildRetrievalReason (entry: MemoryEntryDto) (keyword: string option) =
    let reason = kindLabel entry.Kind

    match keyword with
    | None | Some "" -> reason
    | Some kw when
        containsIgnoreCase entry.Summary kw
        || containsIgnoreCase entry.Key kw
        || containsIgnoreCase entry.Stage kw ->
        $"{reason};keyword_match"
    | _ -> reason

let kindLabelPublic (kind: int) = kindLabel kind
