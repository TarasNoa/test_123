module Libr4.IDE.AutonomousAppGeneration.Algorithms.Playbook.RepairPlaybookSignature

open System
open System.Collections.Generic
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.RegularExpressions

let private stopWords =
    HashSet<string>(
        [| "the"; "and"; "for"; "with"; "from"; "that"; "this"; "error"; "failed"; "cannot"; "could"; "not" |],
        StringComparer.OrdinalIgnoreCase)

let private hashRaw (raw: string) =
    let bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw.Trim().ToLowerInvariant()))
    Convert.ToHexString(bytes).ToLowerInvariant()

let private normalizeFilePattern (filePath: string option) =
    match filePath with
    | None | Some "" -> "*"
    | Some path ->
        let normalized = path.Replace('\\', '/').Trim().ToLowerInvariant()
        let fileName = Path.GetFileName(normalized)
        if String.IsNullOrWhiteSpace fileName then "*"
        else
            let ext = Path.GetExtension(fileName)
            if String.IsNullOrWhiteSpace ext then fileName else $"*{ext}"

let private extractKeywords (message: string) =
    Regex.Matches(message.ToLowerInvariant(), @"[a-z0-9_./-]{4,}")
    |> Seq.map (fun m -> m.Value)
    |> Seq.filter (fun t -> not (stopWords.Contains t))
    |> Seq.distinct
    |> Seq.sort
    |> Seq.truncate 6
    |> String.concat ","

let buildStackPattern (applicationName: string option) (languages: string[]) (frameworks: string[]) =
    match applicationName with
    | None | Some "" -> "unknown"
    | Some name ->
        let langs = String.Join(',', languages)
        let fw = String.Join(',', frameworks)
        $"{name}|{langs}|{fw}".ToLowerInvariant()

let fromError (errorType: string) (filePath: string option) (message: string) =
    let filePattern = normalizeFilePattern filePath
    let keywords = extractKeywords message
    hashRaw $"{errorType}|{filePattern}|{keywords}"

let fromBuildLog (buildLog: string) =
    let line =
        buildLog.Replace('\r', '\n').Split('\n', StringSplitOptions.RemoveEmptyEntries)
        |> Array.tryFind (fun l ->
            l.Contains("error", StringComparison.OrdinalIgnoreCase)
            || l.Contains("failed", StringComparison.OrdinalIgnoreCase)
            || l.Contains("exception", StringComparison.OrdinalIgnoreCase))
        |> Option.defaultValue buildLog
    let keywords = extractKeywords line
    hashRaw $"build|*|{keywords}"

let fromErrors
    (errors: (string * string option * string)[])
    (buildLog: string option)
    (applicationName: string option)
    (languages: string[])
    (frameworks: string[])
    =
    let stackPattern = buildStackPattern applicationName languages frameworks

    let signature =
        if errors.Length > 0 then
            let errorType, filePath, message = errors.[0]
            fromError errorType filePath message
        else
            match buildLog with
            | Some log when not (String.IsNullOrWhiteSpace log) -> fromBuildLog log
            | _ -> hashRaw "unknown|*|build_failure"

    signature, stackPattern
