module SkillNormalizer

open System

let private synonymGroups =
    [|
        [| "javascript"; "js"; "ecmascript"; "es6"; "es2020"; "es2022" |]
        [| "typescript"; "ts" |]
        [| "react"; "reactjs"; "react.js" |]
        [| "node"; "nodejs"; "node.js" |]
        [| "csharp"; "c#"; "dotnet"; "asp.net"; ".net"; "aspnet" |]
        [| "fsharp"; "f#" |]
        [| "python"; "python3"; "py" |]
        [| "django"; "django-rest-framework"; "drf" |]
        [| "fastapi"; "fast api" |]
        [| "postgresql"; "postgres"; "pg"; "psql" |]
        [| "mongodb"; "mongo" |]
        [| "redis"; "redis cache" |]
        [| "docker"; "containerization"; "containers" |]
        [| "kubernetes"; "k8s" |]
        [| "aws"; "amazon web services"; "amazon aws" |]
        [| "rust"; "rust-lang" |]
        [| "golang"; "go"; "go-lang" |]
        [| "java"; "java se"; "java ee" |]
        [| "kotlin"; "kotlin jvm" |]
        [| "graphql"; "graph ql" |]
        [| "vue"; "vuejs"; "vue.js" |]
        [| "angular"; "angularjs"; "angular.js" |]
    |]

let private canonicalMap =
    synonymGroups
    |> Array.collect (fun group ->
        let canonical = group.[0]
        group |> Array.map (fun skill -> skill.ToLowerInvariant().Trim(), canonical))
    |> Map.ofArray

let normalize (skill: string) : string =
    if String.IsNullOrWhiteSpace skill then ""
    else
        let lower = skill.Trim().ToLowerInvariant()
        canonicalMap |> Map.tryFind lower |> Option.defaultValue lower

let normalizeAll (skills: string[]) : string[] =
    if isNull skills then [||]
    else
        skills
        |> Array.filter (fun s -> not (String.IsNullOrWhiteSpace s))
        |> Array.map normalize
        |> Array.distinct

let intersection (skillsA: string[]) (skillsB: string[]) : string[] =
    let setA = normalizeAll skillsA |> Set.ofArray
    let setB = normalizeAll skillsB |> Set.ofArray
    Set.intersect setA setB |> Set.toArray
