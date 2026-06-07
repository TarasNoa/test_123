module Libr4.IDE.AutonomousAppGeneration.Algorithms.RepoGraph.TopologicalSorter

open System
open System.Collections.Generic

let sortGenerationOrder (paths: string[]) (edges: (string * string * string)[]) =
    let nodes = HashSet<string>(paths, StringComparer.OrdinalIgnoreCase)
    let indegree = Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    let adj = Dictionary<string, ResizeArray<string>>(StringComparer.OrdinalIgnoreCase)

    for n in paths do
        indegree.[n] <- 0
        adj.[n] <- ResizeArray<string>()

    for fromPath, toPath, _kind in edges do
        if nodes.Contains fromPath && nodes.Contains toPath then
            adj.[toPath].Add(fromPath)
            indegree.[fromPath] <- indegree.[fromPath] + 1

    let zeroNodes =
        indegree
        |> Seq.filter (fun kv -> kv.Value = 0)
        |> Seq.map (fun kv -> kv.Key)
        |> Seq.toList
        |> List.sortWith (fun a b -> StringComparer.OrdinalIgnoreCase.Compare(a, b))

    let queue = Queue<string>(zeroNodes)
    let ordered = ResizeArray<string>()

    while queue.Count > 0 do
        let node = queue.Dequeue()
        ordered.Add(node)
        for dependent in adj.[node] do
            indegree.[dependent] <- indegree.[dependent] - 1
            if indegree.[dependent] = 0 then
                queue.Enqueue(dependent)

    if ordered.Count < nodes.Count then
        let missing =
            nodes
            |> Seq.filter (fun n -> not (ordered.Contains n))
            |> Seq.toList
            |> List.sortWith (fun a b -> StringComparer.OrdinalIgnoreCase.Compare(a, b))
        for m in missing do
            ordered.Add(m)

    ordered.ToArray()

let sortRepairOrder (generationOrder: string[]) =
    generationOrder |> Array.rev
