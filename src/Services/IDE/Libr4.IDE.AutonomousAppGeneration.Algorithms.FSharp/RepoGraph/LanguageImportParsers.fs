module Libr4.IDE.AutonomousAppGeneration.Algorithms.RepoGraph.LanguageImportParsers

open System
open System.Collections.Generic
open System.IO
open System.Text.RegularExpressions

let private pythonImport = Regex(@"^\s*import\s+([A-Za-z0-9_\.]+)", RegexOptions.Multiline)
let private pythonFromImport = Regex(@"^\s*from\s+([A-Za-z0-9_\.]+)\s+import", RegexOptions.Multiline)
let private tsImport = Regex(@"import\s+[^'""]*['""](\.[^'""]+)['""]", RegexOptions.Multiline)
let private csharpUsing = Regex(@"^\s*using\s+([A-Za-z0-9_\.]+)\s*;", RegexOptions.Multiline)
let private javaImport = Regex(@"^\s*import\s+([A-Za-z0-9_\.]+)\s*;", RegexOptions.Multiline)
let private goImport = Regex(@"import\s+""([^""]+)""", RegexOptions.Multiline)

let private normalizeRelativeImport (importPath: string) =
    let cleaned = importPath.Replace('\\', '/')
    let ext = Path.GetExtension(cleaned)
    let trimmed =
        if ext.Equals(".ts", StringComparison.OrdinalIgnoreCase)
           || ext.Equals(".tsx", StringComparison.OrdinalIgnoreCase)
           || ext.Equals(".js", StringComparison.OrdinalIgnoreCase)
           || ext.Equals(".jsx", StringComparison.OrdinalIgnoreCase) then
            cleaned.Substring(0, cleaned.Length - ext.Length)
        else
            cleaned
    trimmed.TrimStart('.').TrimStart('/')

let private cppInclude = Regex(@"^\s*#\s*include\s+[<""]([^>""]+)[>""]", RegexOptions.Multiline)

let private parseCpp (content: string) =
    let results = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    for m in cppInclude.Matches(content) do
        let path = m.Groups.[1].Value.Trim()
        if path.Length > 0 then
            results.Add(path) |> ignore
    results |> Seq.toArray

let private parsePython (content: string) =
    let results = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    for m in pythonImport.Matches(content) do
        let moduleName = m.Groups.[1].Value.Trim()
        if moduleName.Length > 0 then
            results.Add(moduleName.Replace('.', '/')) |> ignore
    for m in pythonFromImport.Matches(content) do
        let moduleName = m.Groups.[1].Value.Trim()
        if moduleName.Length > 0 then
            results.Add(moduleName.Replace('.', '/')) |> ignore
    results |> Seq.toArray

let private parseTypeScript (content: string) =
    let results = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    for m in tsImport.Matches(content) do
        let path = m.Groups.[1].Value.Trim().Trim('"', '\'')
        if path.Length > 0 && path.[0] = '.' then
            results.Add(normalizeRelativeImport path) |> ignore
    results |> Seq.toArray

let private parseCSharp (content: string) =
    let results = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    for m in csharpUsing.Matches(content) do
        let ns = m.Groups.[1].Value.Trim()
        if ns.Length > 0 then
            results.Add(ns.Replace('.', '/')) |> ignore
    results |> Seq.toArray

let private parseJava (content: string) =
    let results = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    for m in javaImport.Matches(content) do
        let pkg = m.Groups.[1].Value.Trim()
        if pkg.Length > 0 then
            results.Add(pkg.Replace('.', '/')) |> ignore
    results |> Seq.toArray

let private parseGo (content: string) =
    let results = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    for m in goImport.Matches(content) do
        let path = m.Groups.[1].Value.Trim().Trim('"')
        if path.Length > 0 then
            results.Add(path) |> ignore
    results |> Seq.toArray

/// Parse import paths from file content based on extension.
let parseImports (relativePath: string) (content: string option) =
    match content with
    | None -> Array.empty
    | Some c when String.IsNullOrWhiteSpace c -> Array.empty
    | Some c ->
        let ext = Path.GetExtension(relativePath).ToLowerInvariant()
        match ext with
        | ".py" -> parsePython c
        | ".ts" | ".tsx" | ".js" | ".jsx" -> parseTypeScript c
        | ".cs" -> parseCSharp c
        | ".java" -> parseJava c
        | ".go" -> parseGo c
        | ".c" | ".cc" | ".cpp" | ".cxx" | ".h" | ".hh" | ".hpp" | ".hxx" | ".m" | ".mm" -> parseCpp c
        | _ -> Array.empty
