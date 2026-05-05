module Libr4.IDE.AutonomousAppGeneration.Rules.Domain

/// P2-7 of audit roadmap.
/// Represents the target tech stack a rule applies to.
type Stack =
    | DotNet
    | Python
    | Node
    | Any

/// A single file as seen by a rule: relative path + text content.
type FileInput = { RelativePath: string; Content: string }

/// Outcome produced by a rule evaluation.
type RuleOutcome =
    | Pass of checkId: string * detail: string option
    | Fail of checkId: string * reason: string * hint: string option

/// The two flavours of architecture check rule:
///   StackSpecific – applies only when the plan's stack matches
///   Cross         – always evaluated regardless of stack
type Rule =
    | StackSpecific of stack: Stack * checkId: string * evaluate: (FileInput list -> RuleOutcome)
    | Cross         of checkId: string * evaluate: (FileInput list -> RuleOutcome)

/// Helpers for building Rule values and running them.
[<RequireQualifiedAccess>]
module Rule =

    let checkId = function
        | StackSpecific (_, id, _) -> id
        | Cross (id, _) -> id

    let appliesTo (stack: Stack) = function
        | StackSpecific (s, _, _) -> s = Any || s = stack
        | Cross _ -> true

    let evaluate (files: FileInput list) = function
        | StackSpecific (_, _, fn) -> fn files
        | Cross (_, fn) -> fn files

    /// Convenience: check if any file contains a substring (case-insensitive).
    let anyFileContains (token: string) (files: FileInput list) =
        files |> List.exists (fun f ->
            f.Content.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)

    /// Returns true when at least one file ends with the given extension.
    let anyFileByExt (ext: string) (files: FileInput list) =
        files |> List.exists (fun f ->
            f.RelativePath.EndsWith(ext, System.StringComparison.OrdinalIgnoreCase))
