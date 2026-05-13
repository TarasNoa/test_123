namespace Libr4.Education.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Application.Abstractions

// Skill Level Types
type SkillLevel = Beginner = 0 | Intermediate = 1 | Advanced = 2 | Expert = 3 | Master = 4

// Skill Calibration Algorithms
module SkillCalibrator =

    // Calculate skill level based on completed courses, projects, and assessments using AI
    let calculateSkillLevel (aiService: IAIService) (completedCourses: int) (completedProjects: int) (assessmentScore: float) : Async<SkillLevel> =
        async {
            let prompt = sprintf "Calculate skill level: courses %d, projects %d, assessment score %.1f. Return JSON: {\"skillLevel\": \"Beginner/Intermediate/Advanced/Expert/Master\"}" completedCourses completedProjects assessmentScore
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "education") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let skillLevelStr = 
                try root.GetProperty("skillLevel").GetString()
                with _ ->
                    let totalScore = float completedCourses * 10.0 + float completedProjects * 15.0 + assessmentScore
                    match totalScore with
                    | score when score < 30.0 -> "Beginner"
                    | score when score < 60.0 -> "Intermediate"
                    | score when score < 90.0 -> "Advanced"
                    | score when score < 120.0 -> "Expert"
                    | _ -> "Master"
            
            return 
                match skillLevelStr with
                | "Beginner" -> SkillLevel.Beginner
                | "Intermediate" -> SkillLevel.Intermediate
                | "Advanced" -> SkillLevel.Advanced
                | "Expert" -> SkillLevel.Expert
                | _ -> SkillLevel.Master
        }

    // Calculate skill proficiency score (0-10)
    let calculateProficiencyScore (skillLevel: SkillLevel) (experienceYears: int) (recentActivityScore: float) : int =
        let levelScore = int skillLevel * 2
        let experienceScore = min 3 experienceYears
        let activityScore = int (recentActivityScore / 10.0)
        min 10 (levelScore + experienceScore + activityScore)

    // Recommend next learning path based on current skill level using AI
    let recommendLearningPath (aiService: IAIService) (skillLevel: SkillLevel) (skillCategory: string) : Async<string list> =
        async {
            let skillLevelStr = string skillLevel
            let prompt = sprintf "Recommend learning path for %s level in %s. Return JSON: {\"courses\": [string, string, string]}" skillLevelStr skillCategory
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "education") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let courses = 
                try
                    root.GetProperty("courses").EnumerateArray()
                    |> Seq.map (fun c -> c.GetString())
                    |> List.ofSeq
                with _ ->
                    match skillLevel with
                    | SkillLevel.Beginner ->
                        [
                            $"{skillCategory} Fundamentals"
                            $"{skillCategory} Basics"
                            $"Introduction to {skillCategory}"
                        ]
                    | SkillLevel.Intermediate ->
                        [
                            $"{skillCategory} Intermediate Concepts"
                            $"Advanced {skillCategory} Techniques"
                            $"{skillCategory} Best Practices"
                        ]
                    | SkillLevel.Advanced ->
                        [
                            $"{skillCategory} Advanced Patterns"
                            $"Expert {skillCategory} Strategies"
                            $"{skillCategory} Architecture"
                        ]
                    | SkillLevel.Expert ->
                        [
                            $"{skillCategory} Mastery"
                            $"{skillCategory} Innovation"
                            $"Leading {skillCategory} Teams"
                        ]
                    | SkillLevel.Master ->
                        [
                            $"{skillCategory} Research & Development"
                            $"{skillCategory} Industry Leadership"
                            $"{skillCategory} Thought Leadership"
                        ]
            
            return courses
        }

// Skill Verification Algorithms
module SkillVerifier =

    // Verify skill based on completed assessments
    let verifySkillByAssessment (assessmentResults: (string * float) list) (requiredScore: float) : bool =
        match assessmentResults with
        | [] -> false
        | results ->
            let averageScore = results |> List.averageBy snd
            averageScore >= requiredScore

    // Verify skill based on portfolio projects
    let verifySkillByPortfolio (projectCount: int) (averageProjectRating: float) (threshold: int) (ratingThreshold: float) : bool =
        projectCount >= threshold && averageProjectRating >= ratingThreshold

    // Calculate skill confidence score (0-100) using AI
    let calculateSkillConfidence (aiService: IAIService) (assessmentScore: float) (portfolioScore: float) (peerReviews: float) : Async<float> =
        async {
            let prompt = sprintf "Calculate skill confidence: assessment %.1f, portfolio %.1f, peer reviews %.1f. Return JSON: {\"confidence\": number (0-100)}" assessmentScore portfolioScore peerReviews
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "education") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let confidence = 
                try root.GetProperty("confidence").GetDouble()
                with _ ->
                    let weights = [0.4; 0.3; 0.3]
                    let scores = [assessmentScore; portfolioScore; peerReviews]
                    List.zip weights scores
                    |> List.sumBy (fun (w, s) -> w * s)
            
            return confidence
        }

// Progress Tracking Algorithms
module ProgressTracker =

    // Calculate course progress percentage
    let calculateCourseProgress (completedModules: int) (totalModules: int) : float =
        if totalModules = 0 then 0.0
        else float completedModules / float totalModules * 100.0

    // Calculate learning velocity (modules per week)
    let calculateLearningVelocity (completedModules: int) (weeksSpent: int) : float =
        if weeksSpent = 0 then 0.0
        else float completedModules / float weeksSpent

    // Estimate completion time (in weeks)
    let estimateCompletionTime (remainingModules: int) (learningVelocity: float) : int =
        if learningVelocity = 0.0 then 999 // Indefinite
        else int (ceil (float remainingModules / learningVelocity))

    // Calculate engagement score based on activity
    let calculateEngagementScore (loginDays: int) (timeSpentHours: float) (interactions: int) : float =
        let loginScore = min 50.0 (float loginDays * 2.0)
        let timeScore = min 30.0 (timeSpentHours * 2.0)
        let interactionScore = min 20.0 (float interactions * 0.5)
        loginScore + timeScore + interactionScore

// Certification Algorithms
module CertificationEngine =

    // Determine if user is eligible for certification
    let checkCertificationEligibility (courseProgress: float) (assessmentScore: float) (minProgress: float) (minScore: float) : bool =
        courseProgress >= minProgress && assessmentScore >= minScore

    // Calculate certification score (weighted average)
    let calculateCertificationScore (courseWeight: float) (assessmentWeight: float) (projectWeight: float)
                                        (courseScore: float) (assessmentScore: float) (projectScore: float) : float =
        let totalWeight = courseWeight + assessmentWeight + projectWeight
        (courseScore * courseWeight + assessmentScore * assessmentWeight + projectScore * projectWeight) / totalWeight

    // Determine certification level
    let determineCertificationLevel (score: float) : string =
        match score with
        | s when s >= 90.0 -> "Distinction"
        | s when s >= 75.0 -> "Merit"
        | s when s >= 60.0 -> "Pass"
        | _ -> "Fail"

// Skill Gap Analysis Algorithms
module SkillGapAnalyzer =

    // Analyze skill gaps between current level and target level using AI
    let analyzeSkillGaps (aiService: IAIService) (currentSkills: Map<string, int>) (requiredSkills: Map<string, int>) : Async<(string * int) list> =
        async {
            let currentSkillsText = currentSkills |> Map.toList |> List.map (fun (k, v) -> sprintf "%s:%d" k v) |> String.concat ", "
            let requiredSkillsText = requiredSkills |> Map.toList |> List.map (fun (k, v) -> sprintf "%s:%d" k v) |> String.concat ", "
            
            let prompt = sprintf "Analyze skill gaps: current [%s], required [%s]. Return JSON: {\"gaps\": [{\"skill\": string, \"gap\": number}]}" currentSkillsText requiredSkillsText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "education") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let gaps = 
                try
                    root.GetProperty("gaps").EnumerateArray()
                    |> Seq.map (fun g -> (g.GetProperty("skill").GetString(), g.GetProperty("gap").GetInt32()))
                    |> List.ofSeq
                with _ ->
                    requiredSkills
                    |> Map.toList
                    |> List.map (fun (skill, requiredLevel) ->
                        let currentLevel = match currentSkills.TryFind skill with Some level -> level | None -> 0
                        let gap = requiredLevel - currentLevel
                        (skill, max 0 gap))
                    |> List.filter (fun (_, gap) -> gap > 0)
            
            return gaps
        }

    // Calculate overall skill coverage percentage
    let calculateSkillCoverage (currentSkills: Map<string, int>) (requiredSkills: Map<string, int>) : float =
        let requiredCount = Map.count requiredSkills
        if requiredCount = 0 then 0.0
        else
            let coveredCount = 
                currentSkills
                |> Map.toList
                |> List.filter (fun (skill, level) ->
                    match requiredSkills.TryFind skill with
                    | Some requiredLevel -> level >= requiredLevel
                    | None -> false)
                |> List.length
            float coveredCount / float requiredCount * 100.0

    // Recommend skills to prioritize for learning using AI
    let prioritizeSkillsForLearning (aiService: IAIService) (skillGaps: (string * int) list) (careerGoal: string) : Async<(string * int) list> =
        async {
            let gapsText = skillGaps |> List.map (fun (s, g) -> sprintf "%s:%d" s g) |> String.concat ", "
            let prompt = sprintf "Prioritize skills for learning: gaps [%s], career goal '%s'. Return JSON: {\"priorities\": [{\"skill\": string, \"priority\": number (1-10)}]}" gapsText careerGoal
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "education") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let priorities = 
                try
                    root.GetProperty("priorities").EnumerateArray()
                    |> Seq.map (fun p -> (p.GetProperty("skill").GetString(), p.GetProperty("priority").GetInt32()))
                    |> List.ofSeq
                with _ ->
                    skillGaps
                    |> List.sortByDescending snd
                    |> List.take 5
            
            return priorities
        }
