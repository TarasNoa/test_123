namespace Libr4.AI.Domain.TaskRecommendations.Algorithms

open System
open System.Text.Json
open Libr4.AI.Domain.TaskRecommendations
open Libr4.AI.Infrastructure.AI

// Skill Matcher
module SkillMatcher =

    type SkillMatchResult = {
        MatchScore: float32
        MatchingSkills: string list
        MissingSkills: string list
    }

    let matchSkills (aiService: IAIService) (userSkills: string list) (taskSkills: string list) : Async<SkillMatchResult> =
        async {
            let systemPrompt = "You are a skills matching expert. Compare user skills with task requirements and return match in JSON format: {\"matchScore\": number (0-100), \"matchingSkills\": string[], \"missingSkills\": string[]}"
            let userSkillsText = String.concat ", " userSkills
            let taskSkillsText = String.concat ", " taskSkills
            let prompt = sprintf "Match user skills [%s] with task requirements [%s]" userSkillsText taskSkillsText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "skills") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let matchScore = root.GetProperty("matchScore").GetSingle()
            let matchingSkills = 
                root.GetProperty("matchingSkills").EnumerateArray()
                |> Seq.map (fun s -> s.GetString())
                |> List.ofSeq
            let missingSkills = 
                root.GetProperty("missingSkills").EnumerateArray()
                |> Seq.map (fun s -> s.GetString())
                |> List.ofSeq
            
            return {
                MatchScore = matchScore
                MatchingSkills = matchingSkills
                MissingSkills = missingSkills
            }
        }

// Interest Scorer
module InterestScorer =

    type InterestScore = {
        Score: float32
        MatchingInterests: string list
    }

    let scoreInterests (aiService: IAIService) (userInterests: string list) (taskCategories: string list) : Async<InterestScore> =
        async {
            let systemPrompt = "You are an interest matching expert. Compare user interests with task categories and return match in JSON format: {\"score\": number (0-100), \"matchingInterests\": string[]}"
            let userInterestsText = String.concat ", " userInterests
            let taskCategoriesText = String.concat ", " taskCategories
            let prompt = sprintf "Match user interests [%s] with task categories [%s]" userInterestsText taskCategoriesText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "interests") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let score = root.GetProperty("score").GetSingle()
            let matchingInterests = 
                root.GetProperty("matchingInterests").EnumerateArray()
                |> Seq.map (fun i -> i.GetString())
                |> List.ofSeq
            
            return {
                Score = score
                MatchingInterests = matchingInterests
            }
        }

// Recommendation Ranker
module RecommendationRanker =

    type RankedRecommendation = {
        TaskId: Guid
        TaskTitle: string
        OverallScore: float32
        SkillMatch: float32
        InterestScore: float32
        Reason: string
    }

    let rankRecommendations (aiService: IAIService) (tasks: (Guid * string * string list * string list) list) (userSkills: string list) (userInterests: string list) (userRating: float32) : Async<RankedRecommendation list> =
        async {
            let systemPrompt = "You are a recommendation ranking expert. Rank tasks based on skill match, interest alignment, and user history. Return ranked list in JSON array format: [{\"taskId\": string, \"taskTitle\": string, \"overallScore\": number, \"skillMatch\": number, \"interestScore\": number, \"reason\": string}]"
            
            let! recommendationsArray = 
                tasks
                |> List.map (fun (taskId, title, taskSkills, taskCategories) ->
                    async {
                        let! skillMatch = SkillMatcher.matchSkills aiService userSkills taskSkills
                        let! interestScore = InterestScorer.scoreInterests aiService userInterests taskCategories
                        
                        let overallScore = skillMatch.MatchScore * 0.6f + interestScore.Score * 0.3f + userRating * 10f * 0.1f |> min 100f
                        
                        let reason = 
                            sprintf "Skill match: %.1f%%, Interest match: %.1f%%, User rating: %.1f" skillMatch.MatchScore interestScore.Score userRating
                        
                        return {
                            TaskId = taskId
                            TaskTitle = title
                            OverallScore = overallScore
                            SkillMatch = skillMatch.MatchScore
                            InterestScore = interestScore.Score
                            Reason = reason
                        }
                    })
                |> Async.Parallel
            
            let recommendations = Array.toList recommendationsArray
            
            let ranked = 
                recommendations
                |> List.filter (fun r -> r.OverallScore > 30f)
                |> List.sortByDescending (fun r -> r.OverallScore)
            
            return ranked
        }
