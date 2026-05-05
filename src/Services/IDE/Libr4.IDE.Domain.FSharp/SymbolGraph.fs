namespace Libr4.IDE.Domain.FSharp

open System
open System.Collections.Generic

// ============================================================================
// SYMBOL GRAPH — F# functional core for codebase dependency analysis
// Analogous to SocratiCode code-graph + graph-symbols + graph-impact
// ============================================================================

/// Language supported for symbol extraction
type Language =
    | CSharp
    | FSharp
    | Rust
    | TypeScript
    | JavaScript
    | Python
    | Go
    | Java
    | Kotlin
    | Unknown of string

/// Kind of symbol in the graph
type SymbolKind =
    | Class
    | Interface
    | Record
    | Struct
    | Enum
    | Method
    | Function
    | Property
    | Field
    | Constructor
    | Module
    | Namespace

/// A symbol node in the codebase graph
type Symbol = {
    Id: string            // unique: "FilePath::SymbolName"
    Name: string
    FullyQualifiedName: string
    Kind: SymbolKind
    Language: Language
    FilePath: string
    StartLine: int
    EndLine: int
    Modifiers: string list
    DocComment: string option
}

/// A directed edge: Caller → Callee
type CallEdge = {
    FromSymbolId: string
    ToSymbolId: string
    EdgeKind: EdgeKind
    Line: int
}

and EdgeKind =
    | DirectCall
    | MethodOverride
    | InterfaceImplementation
    | Import
    | Instantiation
    | TypeReference

/// Import/dependency edge between files
type ImportEdge = {
    FromFile: string
    ToFile: string
    Alias: string option
    Line: int
}

/// Complete symbol graph for a project
type SymbolGraph = {
    ProjectPath: string
    Symbols: Map<string, Symbol>
    CallEdges: CallEdge list
    ImportEdges: ImportEdge list
    BuildAt: DateTimeOffset
    FileCount: int
    SymbolCount: int
}

/// Impact analysis result (blast radius)
type ImpactNode = {
    SymbolId: string
    FilePath: string
    SymbolName: string
    DistanceFromRoot: int
    EdgeKind: EdgeKind
}

/// Symbol search result
type SymbolSearchResult = {
    Symbol: Symbol
    Score: float
    MatchReason: string
}

// ============================================================================
// Graph construction helpers
// ============================================================================

module SymbolId =
    let make (filePath: string) (symbolName: string) =
        sprintf "%s::%s" filePath symbolName

    let parseFile (id: string) =
        let parts = id.Split([| "::" |], 2, StringSplitOptions.None)
        if parts.Length >= 1 then parts.[0] else id

    let parseName (id: string) =
        let parts = id.Split([| "::" |], 2, StringSplitOptions.None)
        if parts.Length >= 2 then parts.[1] else id

module LanguageDetection =
    let fromExtension (ext: string) =
        match ext.ToLowerInvariant().TrimStart('.') with
        | "cs"  -> CSharp
        | "fs" | "fsi" | "fsx" -> FSharp
        | "rs"  -> Rust
        | "ts" | "tsx" -> TypeScript
        | "js" | "jsx" -> JavaScript
        | "py"  -> Python
        | "go"  -> Go
        | "java" -> Java
        | "kt" | "kts" -> Kotlin
        | other -> Unknown other

    let isCSharpOrFSharp lang =
        match lang with
        | CSharp | FSharp -> true
        | _ -> false

// ============================================================================
// Topological sort (Kahn's algorithm)
// ============================================================================

module TopologicalSort =
    /// Returns files in dependency order (dependencies first).
    /// If cycles exist, returns them separately.
    let sortFiles (importEdges: ImportEdge list) (allFiles: string list) =
        let inDegree = Dictionary<string, int>()
        let adjacency = Dictionary<string, string list>()

        for f in allFiles do
            inDegree.[f] <- 0
            adjacency.[f] <- []

        for edge in importEdges do
            if allFiles |> List.contains edge.FromFile && allFiles |> List.contains edge.ToFile then
                adjacency.[edge.ToFile] <- edge.FromFile :: adjacency.[edge.ToFile]
                inDegree.[edge.FromFile] <- inDegree.GetValueOrDefault(edge.FromFile, 0) + 1

        let queue = Queue<string>()
        for f in allFiles do
            if inDegree.GetValueOrDefault(f, 0) = 0 then queue.Enqueue(f)

        let sorted = System.Collections.Generic.List<string>()
        while queue.Count > 0 do
            let node = queue.Dequeue()
            sorted.Add(node)
            for dependent in adjacency.GetValueOrDefault(node, []) do
                let newDegree = inDegree.[dependent] - 1
                inDegree.[dependent] <- newDegree
                if newDegree = 0 then queue.Enqueue(dependent)

        let cycleFiles = allFiles |> List.filter (fun f -> not (sorted.Contains(f)))
        (sorted |> Seq.toList, cycleFiles)

// ============================================================================
// Impact Analysis — BFS reverse-call traversal (blast radius)
// ============================================================================

module ImpactAnalysis =
    /// Compute blast radius: all symbols that would break if symbolId changes.
    /// Returns nodes in BFS order (closest first).
    let computeBlastRadius
        (graph: SymbolGraph)
        (rootSymbolId: string)
        (maxDepth: int) : ImpactNode list =

        // Build reverse call index: callee → list of callers
        let reverseIndex = Dictionary<string, (string * EdgeKind) list>()
        for edge in graph.CallEdges do
            let current = reverseIndex.GetValueOrDefault(edge.ToSymbolId, [])
            reverseIndex.[edge.ToSymbolId] <- (edge.FromSymbolId, edge.EdgeKind) :: current

        let visited = HashSet<string>()
        let result = System.Collections.Generic.List<ImpactNode>()
        let bfsQueue = Queue<string * int * EdgeKind>()

        bfsQueue.Enqueue((rootSymbolId, 0, DirectCall))
        visited.Add(rootSymbolId) |> ignore

        while bfsQueue.Count > 0 do
            let (symbolId, depth, edgeKind) = bfsQueue.Dequeue()

            if depth > 0 then
                match graph.Symbols |> Map.tryFind symbolId with
                | Some sym ->
                    result.Add({
                        SymbolId = symbolId
                        FilePath = sym.FilePath
                        SymbolName = sym.Name
                        DistanceFromRoot = depth
                        EdgeKind = edgeKind
                    })
                | None ->
                    result.Add({
                        SymbolId = symbolId
                        FilePath = SymbolId.parseFile symbolId
                        SymbolName = SymbolId.parseName symbolId
                        DistanceFromRoot = depth
                        EdgeKind = edgeKind
                    })

            if depth < maxDepth then
                for (callerId, ek) in reverseIndex.GetValueOrDefault(symbolId, []) do
                    if visited.Add(callerId) then
                        bfsQueue.Enqueue((callerId, depth + 1, ek))

        result |> Seq.toList

    /// Forward flow: trace execution from entry point
    let traceFlow
        (graph: SymbolGraph)
        (entrySymbolId: string)
        (maxDepth: int) : ImpactNode list =

        let forwardIndex = Dictionary<string, (string * EdgeKind) list>()
        for edge in graph.CallEdges do
            let current = forwardIndex.GetValueOrDefault(edge.FromSymbolId, [])
            forwardIndex.[edge.FromSymbolId] <- (edge.ToSymbolId, edge.EdgeKind) :: current

        let visited = HashSet<string>()
        let result = System.Collections.Generic.List<ImpactNode>()
        let bfsQueue = Queue<string * int * EdgeKind>()

        bfsQueue.Enqueue((entrySymbolId, 0, DirectCall))
        visited.Add(entrySymbolId) |> ignore

        while bfsQueue.Count > 0 do
            let (symbolId, depth, edgeKind) = bfsQueue.Dequeue()

            if depth > 0 then
                match graph.Symbols |> Map.tryFind symbolId with
                | Some sym ->
                    result.Add({
                        SymbolId = symbolId
                        FilePath = sym.FilePath
                        SymbolName = sym.Name
                        DistanceFromRoot = depth
                        EdgeKind = edgeKind
                    })
                | None -> ()

            if depth < maxDepth then
                for (calleeId, ek) in forwardIndex.GetValueOrDefault(symbolId, []) do
                    if visited.Add(calleeId) then
                        bfsQueue.Enqueue((calleeId, depth + 1, ek))

        result |> Seq.toList

// ============================================================================
// Graph Statistics
// ============================================================================

module GraphStats =
    type GraphStatistics = {
        TotalSymbols: int
        TotalCallEdges: int
        TotalImportEdges: int
        FileCount: int
        LanguageBreakdown: Map<string, int>
        MostConnectedFiles: (string * int) list    // (file, edge count)
        OrphanFiles: string list                   // no imports and no importers
        CircularDependencies: string list list
        UnresolvedEdgePct: float
    }

    let compute (graph: SymbolGraph) (allFiles: string list) : GraphStatistics =
        let langBreakdown =
            graph.Symbols
            |> Map.toSeq
            |> Seq.map snd
            |> Seq.groupBy (fun s -> sprintf "%A" s.Language)
            |> Seq.map (fun (lang, syms) -> lang, Seq.length syms)
            |> Map.ofSeq

        let fileDegree = Dictionary<string, int>()
        for edge in graph.ImportEdges do
            fileDegree.[edge.FromFile] <- fileDegree.GetValueOrDefault(edge.FromFile, 0) + 1
            fileDegree.[edge.ToFile] <- fileDegree.GetValueOrDefault(edge.ToFile, 0) + 1

        let mostConnected =
            fileDegree
            |> Seq.map (fun kv -> kv.Key, kv.Value)
            |> Seq.sortByDescending snd
            |> Seq.truncate 20
            |> Seq.toList

        let connectedFiles = fileDegree.Keys |> Seq.toList |> Set.ofList
        let orphans = allFiles |> List.filter (fun f -> not (connectedFiles.Contains f))

        let (_, cycles) = TopologicalSort.sortFiles graph.ImportEdges allFiles
        let cyclicGroups = if cycles.IsEmpty then [] else [cycles]

        let knownSymbolIds = graph.Symbols |> Map.keys |> Set.ofSeq
        let unresolvedCount =
            graph.CallEdges
            |> List.filter (fun e -> not (knownSymbolIds.Contains e.ToSymbolId))
            |> List.length
        let unresolvedPct =
            if graph.CallEdges.IsEmpty then 0.0
            else float unresolvedCount / float graph.CallEdges.Length * 100.0

        {
            TotalSymbols = graph.SymbolCount
            TotalCallEdges = graph.CallEdges.Length
            TotalImportEdges = graph.ImportEdges.Length
            FileCount = graph.FileCount
            LanguageBreakdown = langBreakdown
            MostConnectedFiles = mostConnected
            OrphanFiles = orphans
            CircularDependencies = cyclicGroups
            UnresolvedEdgePct = unresolvedPct
        }

// ============================================================================
// BM25 scoring — lightweight keyword relevance (no external deps)
// ============================================================================

module Bm25 =
    let private k1 = 1.2
    let private b = 0.75

    let tokenize (text: string) : string array =
        text.Split([| ' '; '\t'; '\n'; '\r'; '.'; ','; '('; ')'; '<'; '>'; '{'; '}'; '['; ']'; '"'; '\''; ';'; ':'; '/' |],
                   StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun t -> t.ToLowerInvariant())
        |> Array.filter (fun t -> t.Length >= 2)

    let score
        (query: string)
        (document: string)
        (avgDocLength: float)
        (docCount: int)
        (queryTermDf: Map<string, int>) : float =

        let queryTokens = tokenize query
        let docTokens = tokenize document
        let docLength = float docTokens.Length

        let termFreq = Dictionary<string, int>()
        for token in docTokens do
            termFreq.[token] <- termFreq.GetValueOrDefault(token, 0) + 1

        queryTokens
        |> Array.sumBy (fun term ->
            let tf = float (termFreq.GetValueOrDefault(term, 0))
            let df = float (queryTermDf.GetValueOrDefault(term, 1))
            let idf = Math.Log((float docCount - df + 0.5) / (df + 0.5) + 1.0)
            let tfNorm = tf * (k1 + 1.0) / (tf + k1 * (1.0 - b + b * docLength / avgDocLength))
            idf * tfNorm)
