namespace Libr4.AI.Domain.LevelUpgrade.Algorithms

open System
open System.Text.Json
open Libr4.AI.Domain.LevelUpgrade
open Libr4.AI.Infrastructure.AI

// Readiness Calculator
module ReadinessCalculator =

    type ReadinessResult = {
        Score: float32
        CanUpgrade: bool
        MissingRequirements: string list
    }

    // Calculate readiness for level upgrade using AI
    let calculateReadiness (aiService: IAIService) (currentXP: int) (xpToNext: int) (achievements: string list) (courses: string list) (requiredAchievements: string list) (requiredCourses: string list) : Async<ReadinessResult> =
        async {
            let xpProgress = float32 currentXP / float32 xpToNext |> min 1.0f
            
            let achievementProgress = 
                if requiredAchievements.IsEmpty then 1.0f
                else 
                    let unlocked = requiredAchievements |> List.filter (fun ra -> achievements |> List.exists (fun a -> a = ra)) |> List.length
                    float32 unlocked / float32 requiredAchievements.Length
            
            let courseProgress = 
                if requiredCourses.IsEmpty then 1.0f
                else 
                    let completed = requiredCourses |> List.filter (fun rc -> courses |> List.exists (fun c -> c = rc)) |> List.length
                    float32 completed / float32 requiredCourses.Length
            
            let achievementsText = achievements |> String.concat ", "
            let coursesText = courses |> String.concat ", "
            let requiredAchievementsText = requiredAchievements |> String.concat ", "
            let requiredCoursesText = requiredCourses |> String.concat ", "
            
            let prompt = sprintf "Calculate level upgrade readiness: current XP %d/%d, achievements [%s], courses [%s], required achievements [%s], required courses [%s]. Return JSON: {\"score\": number (0-100), \"canUpgrade\": boolean}" currentXP xpToNext achievementsText coursesText requiredAchievementsText requiredCoursesText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "upgrade") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let score = 
                try root.GetProperty("score").GetSingle()
                with _ -> (xpProgress * 0.5f + achievementProgress * 0.3f + courseProgress * 0.2f) * 100f
            
            let canUpgrade = 
                try root.GetProperty("canUpgrade").GetBoolean()
                with _ -> score >= 80f
            
            let missingAchievements = requiredAchievements |> List.filter (fun ra -> not (achievements |> List.exists (fun a -> a = ra)))
            let missingCourses = requiredCourses |> List.filter (fun rc -> not (courses |> List.exists (fun c -> c = rc)))
            let missing = missingAchievements @ missingCourses
            
            return {
                Score = score
                CanUpgrade = canUpgrade
                MissingRequirements = missing
            }
        }

// Requirement Analyzer
module RequirementAnalyzer =

    type Requirement = {
        Name: string
        Type: string // XP, Achievement, Course
        Progress: float32
        Completed: bool
    }

    // Analyze requirements for level upgrade using AI
    let analyzeRequirements (aiService: IAIService) (currentXP: int) (xpToNext: int) (achievements: string list) (courses: string list) (requiredAchievements: string list) (requiredCourses: string list) : Async<Requirement list> =
        async {
            let xpRequirement = {
                Name = "Experience Points"
                Type = "XP"
                Progress = float32 currentXP / float32 xpToNext |> min 1.0f
                Completed = currentXP >= xpToNext
            }
            
            let achievementRequirements = 
                requiredAchievements
                |> List.map (fun ra ->
                    {
                        Name = ra
                        Type = "Achievement"
                        Progress = if achievements |> List.exists (fun a -> a = ra) then 1.0f else 0f
                        Completed = achievements |> List.exists (fun a -> a = ra)
                    })
            
            let courseRequirements = 
                requiredCourses
                |> List.map (fun rc ->
                    {
                        Name = rc
                        Type = "Course"
                        Progress = if courses |> List.exists (fun c -> c = rc) then 1.0f else 0f
                        Completed = courses |> List.exists (fun c -> c = rc)
                    })
            
            let achievementsText = achievements |> String.concat ", "
            let coursesText = courses |> String.concat ", "
            
            let prompt = sprintf "Analyze upgrade requirements and suggest additional requirements: achievements [%s], courses [%s]. Return JSON: {\"suggestedRequirements\": [string, string, string]}" achievementsText coursesText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "upgrade") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let suggestedRequirements = 
                try
                    root.GetProperty("suggestedRequirements").EnumerateArray()
                    |> Seq.map (fun s -> s.GetString())
                    |> List.ofSeq
                with _ -> []
            
            let additionalRequirements = 
                suggestedRequirements
                |> List.map (fun sr -> { Name = sr; Type = "Suggested"; Progress = 0f; Completed = false })
            
            return [xpRequirement] @ achievementRequirements @ courseRequirements @ additionalRequirements
        }

// Level Progress Tracker
module LevelProgressTracker =

    type LevelProgress = {
        CurrentLevel: string
        NextLevel: string
        XPProgress: float32
        OverallProgress: float32
        EstimatedTimeToNext: string
    }

    // Track level progress and estimate time to next level using AI
    let trackProgress (aiService: IAIService) (currentLevel: string) (currentXP: int) (xpToNext: int) (averageXPPerDay: float32) : Async<LevelProgress> =
        async {
            let levels = ["Beginner"; "Intermediate"; "Advanced"; "Expert"; "Master"]
            let currentLevelIndex = levels |> List.tryFindIndex (fun l -> l = currentLevel) |> Option.defaultValue 0
            let nextLevel = if currentLevelIndex < levels.Length - 1 then levels.[currentLevelIndex + 1] else "Max"
            
            let xpProgress = float32 currentXP / float32 xpToNext |> min 1.0f
            let overallProgress = float32 currentLevelIndex / float32 levels.Length
            
            let prompt = sprintf "Estimate time to next level: current level '%s', XP %d/%d, average XP per day %.1f. Return JSON: {\"estimatedDays\": number}" currentLevel currentXP xpToNext averageXPPerDay
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "upgrade") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let daysRemaining = 
                try root.GetProperty("estimatedDays").GetInt32()
                with _ ->
                    if averageXPPerDay > 0f then
                        let remainingXP = max 0 (xpToNext - currentXP)
                        int (float32 remainingXP / averageXPPerDay)
                    else 30
            
            let estimatedTime = 
                if daysRemaining <= 7 then sprintf "%d days" daysRemaining
                elif daysRemaining <= 30 then sprintf "%d weeks" (daysRemaining / 7)
                else sprintf "%d months" (daysRemaining / 30)
            
            return {
                CurrentLevel = currentLevel
                NextLevel = nextLevel
                XPProgress = xpProgress
                OverallProgress = overallProgress
                EstimatedTimeToNext = estimatedTime
            }
        }
