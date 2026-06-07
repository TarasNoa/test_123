module Libr4.IDE.AutonomousAppGeneration.Algorithms.RepoGraph.RepoGraphEngine

open System
open System.Collections.Generic
open System.IO
open LanguageImportParsers
open TopologicalSorter

type RepoFileNodeDto = { RelativePath: string; Language: string }
type RepoDependencyEdgeDto = { FromPath: string; ToPath: string; Kind: string }
type RepoGraphDto = { Files: RepoFileNodeDto[]; Edges: RepoDependencyEdgeDto[] }

let private inferLanguage (path: string) =
    match Path.GetExtension(path).ToLowerInvariant() with
    | ".py" -> "python"
    | ".ts" | ".tsx" -> "typescript"
    | ".js" | ".jsx" -> "javascript"
    | ".cs" -> "csharp"
    | ".java" -> "java"
    | ".go" -> "go"
    | ".c" | ".cc" | ".cpp" | ".cxx" | ".h" | ".hh" | ".hpp" | ".hxx" | ".m" | ".mm" -> "cpp"
    | _ -> "unknown"

let private djangoLayerOrder =
    Map.ofList
        [ "models.py", Array.empty
          "serializers.py", [| "models.py" |]
          "views.py", [| "models.py"; "serializers.py" |]
          "urls.py", [| "views.py" |] ]

let private djangoHeuristicDependencies (path: string) =
    let file = Path.GetFileName(path)
    match Map.tryFind file djangoLayerOrder with
    | None -> Array.empty
    | Some deps ->
        let dir = (Path.GetDirectoryName(path) |> Option.ofObj |> Option.defaultValue "").Replace('\\', '/')
        deps
        |> Array.map (fun dep ->
            if String.IsNullOrEmpty dir then dep else $"{dir}/{dep}")
        |> Array.map (fun p -> p.Replace('\\', '/'))

let private resolveImportToPath (fromPath: string) (importPath: string) (knownPaths: HashSet<string>) =
    let dir = (Path.GetDirectoryName(fromPath) |> Option.ofObj |> Option.defaultValue "").Replace('\\', '/')
    let ext = Path.GetExtension(fromPath)
    let mutable candidate = importPath.Replace('\\', '/')
    if not (candidate.EndsWith(".py", StringComparison.OrdinalIgnoreCase)
            || candidate.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)) then
        candidate <- candidate + ext

    let combined =
        if String.IsNullOrEmpty dir then candidate else $"{dir}/{candidate}"
    let combined = combined.Replace("//", "/")
    if knownPaths.Contains combined then
        Some combined
    else
        let fileName = Path.GetFileName candidate
        knownPaths
        |> Seq.tryFind (fun p ->
            p.EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase)
            || p.Equals(fileName, StringComparison.OrdinalIgnoreCase))

let buildGraph (relativePaths: string[]) (contentsByPath: IReadOnlyDictionary<string, string>) =
    let pathSet = HashSet<string>(relativePaths, StringComparer.OrdinalIgnoreCase)
    let files =
        relativePaths
        |> Array.map (fun p -> { RelativePath = p; Language = inferLanguage p })

    let edges = ResizeArray<RepoDependencyEdgeDto>()
    for path in relativePaths do
        let content =
            if contentsByPath.ContainsKey path then Some contentsByPath.[path] else None
        for importPath in parseImports path content do
            match resolveImportToPath path importPath pathSet with
            | Some target ->
                edges.Add({ FromPath = path; ToPath = target; Kind = "import" })
            | None -> ()

        for dep in djangoHeuristicDependencies path do
            if pathSet.Contains dep then
                edges.Add({ FromPath = path; ToPath = dep; Kind = "heuristic" })

    { Files = files
      Edges = edges.ToArray() }

let orderForGeneration (relativePaths: string[]) (contentsByPath: IReadOnlyDictionary<string, string>) =
    let graph = buildGraph relativePaths contentsByPath
    let edgeTuples = graph.Edges |> Array.map (fun e -> e.FromPath, e.ToPath, e.Kind)
    sortGenerationOrder relativePaths edgeTuples

let orderForRepair (relativePaths: string[]) (contentsByPath: IReadOnlyDictionary<string, string>) =
    orderForGeneration relativePaths contentsByPath |> sortRepairOrder
