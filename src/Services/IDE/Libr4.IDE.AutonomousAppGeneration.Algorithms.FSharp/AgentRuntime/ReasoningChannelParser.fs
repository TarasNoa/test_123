module Libr4.IDE.AutonomousAppGeneration.Algorithms.AgentRuntime.ReasoningChannelParser

open System
open System.Collections.Generic
open System.Text.RegularExpressions

type ReasoningParseResultDto = { VisibleContent: string; ReasoningContent: string option }

let private thinkingRegex =
    Regex(
        @"<think(?:ing)?>([\s\S]*?)</think(?:ing)?>",
        RegexOptions.IgnoreCase ||| RegexOptions.Compiled)

let split (raw: string) =
    if String.IsNullOrWhiteSpace raw then
        { VisibleContent = ""; ReasoningContent = None }
    else
        let reasoning = List<string>()
        let mutable visible = raw

        for m in thinkingRegex.Matches raw do
            reasoning.Add(m.Groups.[1].Value.Trim())
            visible <- visible.Replace(m.Value, "", StringComparison.Ordinal)

        visible <- visible.Trim()

        let combined =
            if reasoning.Count > 0 then Some(String.Join("\n\n", reasoning))
            else None

        { VisibleContent = visible; ReasoningContent = combined }
