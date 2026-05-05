namespace Libr4.AI.Domain.SmartAssistant.Algorithms

open System
open System.Text.Json
open Libr4.AI.Domain.SmartAssistant
open Libr4.AI.Infrastructure.AI

// Context Analyzer
module ContextAnalyzer =

    type ContextAnalysis = {
        Category: string
        Intent: string
        Keywords: string list
        Confidence: float32
    }

    let analyzeContext (aiService: IAIService) (message: string) (sessionType: string) : Async<ContextAnalysis> =
        async {
            let systemPrompt = "You are a context analysis expert. Analyze the user message and return context in JSON format: {\"category\": string, \"intent\": string, \"keywords\": string[], \"confidence\": number (0-1)}"
            let prompt = sprintf "Analyze context for message: %s in session type: %s" message sessionType
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "context") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let category = root.GetProperty("category").GetString()
            let intent = root.GetProperty("intent").GetString()
            let keywords = 
                root.GetProperty("keywords").EnumerateArray()
                |> Seq.map (fun k -> k.GetString())
                |> List.ofSeq
            let confidence = root.GetProperty("confidence").GetSingle()
            
            return {
                Category = category
                Intent = intent
                Keywords = keywords
                Confidence = confidence
            }
        }

// Response Generator
module ResponseGenerator =

    type SuggestedResponse = {
        Response: string
        FollowUpQuestions: string list
        Actions: string list
    }

    let generateResponse (aiService: IAIService) (context: ContextAnalyzer.ContextAnalysis) (userMessage: string) : Async<SuggestedResponse> =
        async {
            let systemPrompt = sprintf "You are a helpful AI assistant for a freelancing platform. The user is asking about: %s. Provide helpful, actionable advice. Return your response in JSON format: {\"response\": string, \"followUpQuestions\": string[], \"actions\": string[]}" context.Category
            
            let! aiResponse = aiService.ChatAsync(userMessage, systemPrompt) |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let response = root.GetProperty("response").GetString()
            let followUpQuestions = 
                root.GetProperty("followUpQuestions").EnumerateArray()
                |> Seq.map (fun q -> q.GetString())
                |> List.ofSeq
            let actions = 
                root.GetProperty("actions").EnumerateArray()
                |> Seq.map (fun a -> a.GetString())
                |> List.ofSeq
            
            return {
                Response = response
                FollowUpQuestions = followUpQuestions
                Actions = actions
            }
        }

// Suggestion Ranker
module SuggestionRanker =

    type RankedSuggestion = {
        Suggestion: string
        Category: string
        RelevanceScore: float32
    }

    let rankSuggestions (aiService: IAIService) (suggestions: (string * string) list) (context: ContextAnalyzer.ContextAnalysis) : Async<RankedSuggestion list> =
        async {
            let systemPrompt = "You are a suggestion ranking expert. Rank suggestions based on relevance to context and return in JSON array format: [{\"suggestion\": string, \"category\": string, \"relevanceScore\": number (0-1)}]"
            let suggestionsText = suggestions |> List.map (fun (s, c) -> sprintf "%s (%s)" s c) |> String.concat "; "
            let contextText = sprintf "Category: %s, Intent: %s, Keywords: %s" context.Category context.Intent (String.concat ", " context.Keywords)
            let prompt = sprintf "Rank these suggestions: %s. Context: %s" suggestionsText contextText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "suggestions") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let ranked = 
                jsonDoc.RootElement.EnumerateArray()
                |> Seq.map (fun element ->
                    {
                        Suggestion = element.GetProperty("suggestion").GetString()
                        Category = element.GetProperty("category").GetString()
                        RelevanceScore = element.GetProperty("relevanceScore").GetSingle()
                    })
                |> List.ofSeq
                |> List.filter (fun s -> s.RelevanceScore > 0.3f)
                |> List.sortByDescending (fun s -> s.RelevanceScore)
            
            return ranked
        }
