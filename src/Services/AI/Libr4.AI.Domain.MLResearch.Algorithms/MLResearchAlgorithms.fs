namespace Libr4.AI.Domain.MLResearch.Algorithms

open System
open System.Text.Json
open Libr4.AI.Domain.MLResearch
open Libr4.AI.Application.Abstractions

// Paper Recommender
module PaperRecommender =

    type PaperMatch = {
        PaperId: string
        Title: string
        RelevanceScore: float32
        Reason: string
    }

    // Recommend ArXiv papers based on research area
    let recommendPapers (researchArea: ResearchArea) (paperTitles: string list) (paperAbstracts: string list) : PaperMatch list =
        let areaKeywords = 
            match researchArea with
            | ResearchArea.NLP -> ["language"; "text"; "transformer"; "bert"; "gpt"; "nlp"; "sentiment"; "translation"]
            | ResearchArea.ComputerVision -> ["vision"; "image"; "cnn"; "object"; "detection"; "segmentation"; "recognition"]
            | ResearchArea.ReinforcementLearning -> ["reinforcement"; "rl"; "agent"; "policy"; "reward"; "q-learning"]
            | ResearchArea.GenerativeAI -> ["generative"; "diffusion"; "gan"; "vae"; "synthetic"; "generation"]
            | ResearchArea.AnomalyDetection -> ["anomaly"; "detection"; "outlier"; "fraud"; "deviation"]
            | ResearchArea.RecommendationSystems -> ["recommendation"; "collaborative"; "filtering"; "ranking"; "personalization"]
            | _ -> ["ml"; "machine"; "learning"; "neural"; "network"]
        
        let matches = 
            List.zip3 paperTitles paperAbstracts (List.init paperTitles.Length (fun i -> string i))
            |> List.map (fun (title, paperAbstract, id) ->
                let combinedText = (title + " " + paperAbstract).ToLower()
                let keywordMatches = 
                    areaKeywords 
                    |> List.filter (fun kw -> combinedText.Contains(kw))
                    |> List.length
                
                let relevanceScore = 
                    if keywordMatches = 0 then 0f
                    else float32 keywordMatches / float32 areaKeywords.Length * 100f |> min 100f
                
                let reason = 
                    if keywordMatches > 0 then 
                        sprintf "Matched %d keywords" keywordMatches
                    else "No keyword match"
                
                {
                    PaperId = id
                    Title = title
                    RelevanceScore = relevanceScore
                    Reason = reason
                })
            |> List.filter (fun m -> m.RelevanceScore > 30f)
            |> List.sortByDescending (fun m -> m.RelevanceScore)
        
        matches

    // Recommend papers using AI for semantic matching
    let recommendPapersWithAI (aiService: IAIService) (researchArea: string) (paperTitles: string list) (paperAbstracts: string list) (researchInterests: string) : Async<PaperMatch list> =
        async {
            let areaStr = string researchArea
            let papersText = List.zip3 paperTitles paperAbstracts (List.init paperTitles.Length (fun i -> string i))
                           |> List.map (fun (title, paperAbstract, id) -> sprintf "%s: %s" id (title + " " + paperAbstract))
                           |> String.concat "; "
            
            let prompt = sprintf "Recommend relevant papers for research area '%s' with interests '%s'. Available papers: [%s]. Return JSON: {\"recommendations\": [{\"paperId\": string, \"title\": string, \"relevanceScore\": number (0-1), \"reason\": string}]}" areaStr researchInterests papersText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "ai") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let recommendations = 
                try
                    root.GetProperty("recommendations").EnumerateArray()
                    |> Seq.map (fun r -> 
                        {
                            PaperId = r.GetProperty("paperId").GetString()
                            Title = r.GetProperty("title").GetString()
                            RelevanceScore = r.GetProperty("relevanceScore").GetSingle()
                            Reason = r.GetProperty("reason").GetString()
                        })
                    |> List.ofSeq
                with _ ->
                    let areaEnum = 
                        match areaStr.ToLower() with
                        | "nlp" -> ResearchArea.NLP
                        | "computervision" -> ResearchArea.ComputerVision
                        | "reinforcementlearning" -> ResearchArea.ReinforcementLearning
                        | "generativeai" -> ResearchArea.GenerativeAI
                        | "anomalydetection" -> ResearchArea.AnomalyDetection
                        | "recommendationsystems" -> ResearchArea.RecommendationSystems
                        | _ -> ResearchArea.NLP
                    recommendPapers areaEnum paperTitles paperAbstracts
            
            return recommendations
        }

// Experiment Tracker
module ExperimentTracker =

    type ExperimentMetrics = {
        TotalExperiments: int
        CompletedExperiments: int
        FailedExperiments: int
        AverageAccuracy: float32
        AverageLoss: float32
        SuccessRate: float32
    }

    // Track ML experiment metrics
    let trackExperiments (experiments: MLExperiment list) : ExperimentMetrics =
        if experiments.IsEmpty then
            {
                TotalExperiments = 0
                CompletedExperiments = 0
                FailedExperiments = 0
                AverageAccuracy = 0f
                AverageLoss = 0f
                SuccessRate = 0f
            }
        else
            let completed = experiments |> List.filter (fun e -> e.Status = ExperimentStatus.Completed)
            let failed = experiments |> List.filter (fun e -> e.Status = ExperimentStatus.Failed)
            
            let avgAccuracy = 
                if completed.IsEmpty then 0f
                else 
                    completed 
                    |> List.choose (fun e -> e.Accuracy |> Option.ofNullable)
                    |> List.average
            
            let avgLoss = 
                if completed.IsEmpty then 0f
                else 
                    completed 
                    |> List.choose (fun e -> e.Loss |> Option.ofNullable)
                    |> List.average
            
            let successRate = 
                if experiments.IsEmpty then 0f
                else float32 completed.Length / float32 experiments.Length * 100f
            
            {
                TotalExperiments = experiments.Length
                CompletedExperiments = completed.Length
                FailedExperiments = failed.Length
                AverageAccuracy = avgAccuracy
                AverageLoss = avgLoss
                SuccessRate = successRate
            }

    // Predict experiment success using AI
    let predictExperimentSuccess (aiService: IAIService) (experimentConfig: string) (historicalMetrics: ExperimentMetrics) (datasetCharacteristics: string) : Async<ExperimentMetrics> =
        async {
            let prompt = sprintf "Predict experiment success: config '%s', historical success %.2f, accuracy %.2f, loss %.2f, dataset '%s'. Return JSON: {\"predictedSuccessRate\": number (0-1), \"predictedAccuracy\": number (0-1), \"predictedLoss\": number}" experimentConfig historicalMetrics.SuccessRate historicalMetrics.AverageAccuracy historicalMetrics.AverageLoss datasetCharacteristics
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "ai") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let predictedSuccessRate = 
                try root.GetProperty("predictedSuccessRate").GetSingle()
                with _ -> historicalMetrics.SuccessRate
            
            let predictedAccuracy = 
                try root.GetProperty("predictedAccuracy").GetSingle()
                with _ -> historicalMetrics.AverageAccuracy
            
            let predictedLoss = 
                try root.GetProperty("predictedLoss").GetSingle()
                with _ -> historicalMetrics.AverageLoss
            
            return {
                TotalExperiments = historicalMetrics.TotalExperiments + 1
                CompletedExperiments = historicalMetrics.CompletedExperiments
                FailedExperiments = historicalMetrics.FailedExperiments
                AverageAccuracy = predictedAccuracy
                AverageLoss = predictedLoss
                SuccessRate = predictedSuccessRate
            }
        }

// Research Area Matcher
module ResearchAreaMatcher =

    type AreaMatch = {
        Area: ResearchArea
        Confidence: float32
        SuggestedExperiments: string list
    }

    // Match research area based on project description
    let matchResearchArea (description: string) : AreaMatch list =
        let descLower = description.ToLower()
        
        let allAreas = [
            (ResearchArea.NLP, ["nlp"; "text"; "language"; "sentiment"; "translation"; "chatbot"; "bert"; "gpt"])
            (ResearchArea.ComputerVision, ["vision"; "image"; "video"; "object"; "detection"; "segmentation"; "recognition"; "cnn"])
            (ResearchArea.ReinforcementLearning, ["reinforcement"; "rl"; "agent"; "policy"; "reward"; "q-learning"; "game"; "control"])
            (ResearchArea.GenerativeAI, ["generative"; "diffusion"; "gan"; "vae"; "synthetic"; "generation"; "create"; "generate"])
            (ResearchArea.AnomalyDetection, ["anomaly"; "detection"; "outlier"; "fraud"; "deviation"; "unusual"; "suspicious"])
            (ResearchArea.RecommendationSystems, ["recommendation"; "collaborative"; "filtering"; "ranking"; "personalization"; "suggest"])
        ]
        
        allAreas
        |> List.map (fun (area, keywords) ->
            let matches = keywords |> List.filter (fun kw -> descLower.Contains(kw)) |> List.length
            let confidence = float32 matches / float32 keywords.Length * 100f |> min 100f
            
            let suggestedExperiments = 
                match area with
                | ResearchArea.NLP -> ["Text Classification"; "Sentiment Analysis"; "Named Entity Recognition"; "Machine Translation"]
                | ResearchArea.ComputerVision -> ["Image Classification"; "Object Detection"; "Semantic Segmentation"; "Image Generation"]
                | ResearchArea.ReinforcementLearning -> ["Q-Learning"; "Policy Gradient"; "Actor-Critic"; "PPO"]
                | ResearchArea.GenerativeAI -> ["GAN Training"; "Diffusion Models"; "VAE Training"; "Style Transfer"]
                | ResearchArea.AnomalyDetection -> ["Autoencoder Anomaly Detection"; "Isolation Forest"; "One-Class SVM"]
                | ResearchArea.RecommendationSystems -> ["Collaborative Filtering"; "Content-Based Filtering"; "Matrix Factorization"; "Deep Learning Recommenders"]
                | _ -> ["Custom Experiment"]
            
            {
                Area = area
                Confidence = confidence
                SuggestedExperiments = suggestedExperiments
            })
        |> List.filter (fun m -> m.Confidence > 20f)
        |> List.sortByDescending (fun m -> m.Confidence)

    // Match research area using AI for semantic understanding
    let matchResearchAreaWithAI (aiService: IAIService) (description: string) (projectGoals: string list) : Async<AreaMatch list> =
        async {
            let goalsText = String.concat ", " projectGoals
            let prompt = sprintf "Match research area for project: description '%s', goals [%s]. Return JSON: {\"matches\": [{\"area\": string, \"confidence\": number (0-1), \"experiments\": [string]}]}" description goalsText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "ai") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let matches = 
                try
                    root.GetProperty("matches").EnumerateArray()
                    |> Seq.map (fun m ->
                        let areaStr = m.GetProperty("area").GetString()
                        let area = 
                            match areaStr.ToLower() with
                            | "nlp" | "language" -> ResearchArea.NLP
                            | "vision" | "image" | "computer" -> ResearchArea.ComputerVision
                            | "reinforcement" | "rl" -> ResearchArea.ReinforcementLearning
                            | "generative" | "diffusion" -> ResearchArea.GenerativeAI
                            | "anomaly" -> ResearchArea.AnomalyDetection
                            | "recommendation" -> ResearchArea.RecommendationSystems
                            | _ -> ResearchArea.NLP
                        
                        let experiments = 
                            m.GetProperty("experiments").EnumerateArray()
                            |> Seq.map (fun e -> e.GetString())
                            |> List.ofSeq
                        
                        {
                            Area = area
                            Confidence = m.GetProperty("confidence").GetSingle()
                            SuggestedExperiments = experiments
                        })
                    |> List.ofSeq
                with _ ->
                    matchResearchArea description
            
            return matches
        }
