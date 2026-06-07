module Libr4.IDE.AutonomousAppGeneration.Algorithms.AgentRuntime.AgentResponseParser

open System
open System.Text.Json
open System.Text.RegularExpressions
open Libr4.IDE.AutonomousAppGeneration.Algorithms.AgentRuntime.ReasoningChannelParser

/// Tool=0, Done=1, Invalid=2
type TurnResponseDto =
    { Action: int
      ToolName: string option
      ToolInputJson: string option
      Summary: string option
      VisibleContent: string
      ReasoningContent: string option
      ErrorMessage: string option }

let private jsonFenceRegex =
    Regex(@"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase ||| RegexOptions.Compiled)

let private extractJsonObject (raw: string) =
    let mutable trimmed = raw.Trim()
    let fence = jsonFenceRegex.Match trimmed

    if fence.Success then
        trimmed <- fence.Groups.[1].Value.Trim()

    let start = trimmed.IndexOf '{'
    let finish = trimmed.LastIndexOf '}'

    if start < 0 || finish <= start then
        None
    else
        Some(trimmed.[start .. finish])

let private readString (root: JsonElement) (name: string) =
    match root.TryGetProperty name with
    | true, el when el.ValueKind = JsonValueKind.String -> Some(el.GetString())
    | _ -> None

let parse (raw: string) (stripReasoning: bool) =
    if String.IsNullOrWhiteSpace raw then
        { Action = 2
          ToolName = None
          ToolInputJson = None
          Summary = None
          VisibleContent = raw
          ReasoningContent = None
          ErrorMessage = None }
    else
        let splitResult = ReasoningChannelParser.split raw
        let visible = if stripReasoning then splitResult.VisibleContent else raw

        match extractJsonObject visible with
        | None ->
            { Action = 2
              ToolName = None
              ToolInputJson = None
              Summary = Some "Could not parse JSON tool response"
              VisibleContent = visible
              ReasoningContent = splitResult.ReasoningContent
              ErrorMessage = None }
        | Some json ->
            try
                use doc = JsonDocument.Parse json
                let root = doc.RootElement

                let action =
                    readString root "action"
                    |> Option.map (fun a -> a.Trim().ToLowerInvariant())

                match action with
                | Some a when a = "done" || a = "complete" || a = "finish" ->
                    { Action = 1
                      ToolName = None
                      ToolInputJson = None
                      Summary = Some (defaultArg (readString root "summary") "Task completed")
                      VisibleContent = visible
                      ReasoningContent = splitResult.ReasoningContent
                      ErrorMessage = None }
                | Some a when a = "tool" || a = "tool_call" ->
                    let toolName =
                        readString root "tool"
                        |> Option.orElseWith (fun () -> readString root "name")
                        |> Option.map (fun t -> t.Trim())

                    match toolName with
                    | None | Some "" ->
                        { Action = 2
                          ToolName = None
                          ToolInputJson = None
                          Summary = Some "Missing tool name"
                          VisibleContent = visible
                          ReasoningContent = splitResult.ReasoningContent
                          ErrorMessage = None }
                    | Some name ->
                        let inputJson =
                            match root.TryGetProperty "input" with
                            | true, inputEl when inputEl.ValueKind = JsonValueKind.Object ->
                                inputEl.GetRawText()
                            | _ -> "{}"

                        { Action = 0
                          ToolName = Some name
                          ToolInputJson = Some inputJson
                          Summary = None
                          VisibleContent = visible
                          ReasoningContent = splitResult.ReasoningContent
                          ErrorMessage = None }
                | Some a ->
                    { Action = 2
                      ToolName = None
                      ToolInputJson = None
                      Summary = Some $"Unknown action '{a}'"
                      VisibleContent = visible
                      ReasoningContent = splitResult.ReasoningContent
                      ErrorMessage = None }
                | None ->
                    { Action = 2
                      ToolName = None
                      ToolInputJson = None
                      Summary = Some "Unknown action ''"
                      VisibleContent = visible
                      ReasoningContent = splitResult.ReasoningContent
                      ErrorMessage = None }
            with :? JsonException as ex ->
                { Action = 2
                  ToolName = None
                  ToolInputJson = None
                  Summary = Some ex.Message
                  VisibleContent = visible
                  ReasoningContent = splitResult.ReasoningContent
                  ErrorMessage = Some ex.Message }
