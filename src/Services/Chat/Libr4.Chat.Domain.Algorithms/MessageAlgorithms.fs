namespace Libr4.Chat.Domain.Algorithms

open System
open System.Text.Json
open Libr4.Chat.Domain.Messages
open Libr4.AI.Infrastructure.AI

// Message Content Analyzer
module MessageContentAnalyzer =

    type ContentAnalysis = {
        SentimentScore: float32
        SentimentLabel: string
        IsSpam: bool
        SpamScore: float32
        ProfessionalToneScore: float32
        ConflictDetected: bool
    }

    // Analyze message content (placeholder for ML integration)
    let analyzeContent (content: string) (messageType: MessageType) : ContentAnalysis =
        // This is a placeholder - in production, this would call ML.NET or external ML service
        let words = content.Split(' ')
        let wordCount = words.Length
        
        // Simple heuristics (to be replaced with true ML per memory rules)
        let sentimentScore = 
            if content.ToLower().Contains("good") || content.ToLower().Contains("great") || content.ToLower().Contains("thanks") then 0.8f
            elif content.ToLower().Contains("bad") || content.ToLower().Contains("terrible") || content.ToLower().Contains("hate") then -0.8f
            else 0.0f
        
        let sentimentLabel = 
            if sentimentScore > 0.3f then "positive"
            elif sentimentScore < -0.3f then "negative"
            else "neutral"
        
        let isSpam = 
            content.ToLower().Contains("buy now") || 
            content.ToLower().Contains("click here") ||
            content.ToLower().Contains("free money") ||
            wordCount > 100 && content.Contains("!!!")
        
        let spamScore = if isSpam then 0.9f else 0.1f
        
        let professionalToneScore = 
            if content.Contains("please") || content.Contains("would you") || content.Contains("thank") then 0.8f
            elif content.Contains("!!!") || content.ToLower().Contains("urgent") then 0.2f
            else 0.5f
        
        let conflictDetected = 
            content.ToLower().Contains("stupid") ||
            content.ToLower().Contains("idiot") ||
            content.ToLower().Contains("hate you") ||
            content.Contains("!!!")
        
        {
            SentimentScore = sentimentScore
            SentimentLabel = sentimentLabel
            IsSpam = isSpam
            SpamScore = spamScore
            ProfessionalToneScore = professionalToneScore
            ConflictDetected = conflictDetected
        }

    // Analyze content using AI
    let analyzeContentWithAI (aiService: IAIService) (content: string) (messageType: string) : Async<ContentAnalysis> =
        async {
            let prompt = sprintf "Analyze message content for chat: '%s', type '%s'. Return JSON: {\"sentimentScore\": number (-1 to 1), \"sentimentLabel\": \"positive/negative/neutral\", \"isSpam\": bool, \"spamScore\": number (0-1), \"professionalToneScore\": number (0-1), \"conflictDetected\": bool}" content messageType
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let sentimentScore = try root.GetProperty("sentimentScore").GetSingle() with _ -> 0.0f
            let sentimentLabel = try root.GetProperty("sentimentLabel").GetString() with _ -> "neutral"
            let isSpam = try root.GetProperty("isSpam").GetBoolean() with _ -> false
            let spamScore = try root.GetProperty("spamScore").GetSingle() with _ -> 0.1f
            let professionalToneScore = try root.GetProperty("professionalToneScore").GetSingle() with _ -> 0.5f
            let conflictDetected = try root.GetProperty("conflictDetected").GetBoolean() with _ -> false
            
            return {
                SentimentScore = sentimentScore
                SentimentLabel = sentimentLabel
                IsSpam = isSpam
                SpamScore = spamScore
                ProfessionalToneScore = professionalToneScore
                ConflictDetected = conflictDetected
            }
        }

// Message Thread Analyzer
module MessageThreadAnalyzer =

    type ThreadSummary = {
        MessageCount: int
        ParticipantCount: int
        AverageSentiment: float32
        ConflictCount: int
        SpamCount: int
        IsHealthy: bool
    }

    // Analyze message thread for health indicators
    let analyzeThread (messages: Message list) : ThreadSummary =
        let messageCount = messages.Length
        let participants = messages |> List.map (fun m -> m.SenderId) |> Set.ofList
        let participantCount = participants.Count
        
        let sentiments = messages |> List.choose (fun m -> m.SentimentScore |> Option.ofNullable)
        let avgSentiment = 
            if sentiments.IsEmpty then 0.0f
            else sentiments |> List.average
        
        let conflictCount = messages |> List.sumBy (fun m -> if m.IsConflictDetected.HasValue && m.IsConflictDetected.Value then 1 else 0)
        let spamCount = messages |> List.sumBy (fun m -> if m.IsSpam.HasValue && m.IsSpam.Value then 1 else 0)
        
        let isHealthy = 
            avgSentiment >= -0.3f && 
            conflictCount < messageCount / 5 &&
            spamCount < messageCount / 10
        
        {
            MessageCount = messageCount
            ParticipantCount = participantCount
            AverageSentiment = avgSentiment
            ConflictCount = conflictCount
            SpamCount = spamCount
            IsHealthy = isHealthy
        }

    // Analyze thread using AI
    let analyzeThreadWithAI (aiService: IAIService) (messages: Message list) : Async<ThreadSummary> =
        async {
            let messageCount = messages.Length
            let participants = messages |> List.map (fun m -> m.SenderId) |> Set.ofList
            let participantCount = participants.Count
            
            let messagesText = messages |> List.map (fun m -> sprintf "Sender %s: %s" (m.SenderId.ToString()) m.Content) |> String.concat " | "
            
            let prompt = sprintf "Analyze chat thread health: %d messages, %d participants. Messages: [%s]. Return JSON: {\"averageSentiment\": number (-1 to 1), \"conflictCount\": number, \"spamCount\": number, \"isHealthy\": bool}" messageCount participantCount messagesText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let avgSentiment = try root.GetProperty("averageSentiment").GetSingle() with _ -> 0.0f
            let conflictCount = try root.GetProperty("conflictCount").GetInt32() with _ -> 0
            let spamCount = try root.GetProperty("spamCount").GetInt32() with _ -> 0
            let isHealthy = try root.GetProperty("isHealthy").GetBoolean() with _ -> true
            
            return {
                MessageCount = messageCount
                ParticipantCount = participantCount
                AverageSentiment = avgSentiment
                ConflictCount = conflictCount
                SpamCount = spamCount
                IsHealthy = isHealthy
            }
        }

// Message Search Engine
module MessageSearchEngine =

    type SearchResult = {
        MessageId: Guid
        RelevanceScore: float
        MatchedTerms: string list
    }

    // Search messages with relevance scoring
    let searchMessages (messages: Message list) (query: string) : SearchResult list =
        let queryTerms = query.ToLower().Split(' ') |> Array.toList
        
        messages
        |> List.map (fun m ->
            let content = m.Content.ToLower()
            let matchedTerms = queryTerms |> List.filter (fun term -> content.Contains(term))
            let relevanceScore = float matchedTerms.Length / float queryTerms.Length
            
            {
                MessageId = m.Id
                RelevanceScore = relevanceScore
                MatchedTerms = matchedTerms
            }
        )
        |> List.filter (fun r -> r.RelevanceScore > 0.0)
        |> List.sortByDescending (fun r -> r.RelevanceScore)

    // Search messages using AI for semantic understanding
    let searchMessagesWithAI (aiService: IAIService) (messages: Message list) (query: string) : Async<SearchResult list> =
        async {
            let messagesText = messages |> List.map (fun m -> sprintf "ID %s: %s" (m.Id.ToString()) m.Content) |> String.concat " | "
            
            let prompt = sprintf "Search messages for query '%s': [%s]. Return JSON: {\"results\": [{\"messageId\": string (guid), \"relevanceScore\": number (0-1), \"matchedTerms\": [string]}]}" query messagesText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let results = 
                try
                    root.GetProperty("results").EnumerateArray()
                    |> Seq.map (fun r ->
                        let messageId = Guid.Parse(r.GetProperty("messageId").GetString())
                        let relevanceScore = r.GetProperty("relevanceScore").GetDouble()
                        let matchedTerms = 
                            r.GetProperty("matchedTerms").EnumerateArray()
                            |> Seq.map (fun t -> t.GetString())
                            |> List.ofSeq
                        
                        {
                            MessageId = messageId
                            RelevanceScore = relevanceScore
                            MatchedTerms = matchedTerms
                        })
                    |> List.ofSeq
                with _ ->
                    searchMessages messages query
            
            return results |> List.sortByDescending (fun r -> r.RelevanceScore)
        }

// Message Reply Analyzer
module MessageReplyAnalyzer =

    type ReplySuggestion = {
        SuggestedResponse: string
        Confidence: float
        Context: string
    }

    // Suggest reply based on message content (placeholder for ML)
    let suggestReply (message: Message) : ReplySuggestion list =
        let content = message.Content.ToLower()
        
        let suggestions = ResizeArray<ReplySuggestion>()
        
        if content.Contains("thank") || content.Contains("thanks") then
            suggestions.Add({
                SuggestedResponse = "You're welcome!"
                Confidence = 0.85
                Context = "Response to gratitude"
            })
        
        if content.Contains("hello") || content.Contains("hi") then
            suggestions.Add({
                SuggestedResponse = "Hello! How can I help you?"
                Confidence = 0.80
                Context = "Greeting response"
            })
        
        if content.Contains("?") then
            suggestions.Add({
                SuggestedResponse = "Let me check that for you."
                Confidence = 0.60
                Context = "Question response"
            })
        
        if suggestions.Count = 0 then
            suggestions.Add({
                SuggestedResponse = "Thanks for your message."
                Confidence = 0.40
                Context = "Generic response"
            })
        
        List.ofSeq suggestions

    // Suggest reply using AI
    let suggestReplyWithAI (aiService: IAIService) (message: Message) (context: string) : Async<ReplySuggestion list> =
        async {
            let prompt = sprintf "Suggest replies for message '%s' in context '%s'. Return JSON: {\"suggestions\": [{\"response\": string, \"confidence\": number (0-1), \"context\": string}]}" message.Content context
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "chat") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let suggestions = 
                try
                    root.GetProperty("suggestions").EnumerateArray()
                    |> Seq.map (fun s ->
                        {
                            SuggestedResponse = s.GetProperty("response").GetString()
                            Confidence = s.GetProperty("confidence").GetDouble()
                            Context = s.GetProperty("context").GetString()
                        })
                    |> List.ofSeq
                with _ ->
                    suggestReply message
            
            return suggestions |> List.sortByDescending (fun s -> s.Confidence)
        }
