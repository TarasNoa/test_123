namespace Libr4.Education.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Application.Abstractions

// Level Progression Calculator
module LevelProgressionCalculator =

    type LevelConfig = {
        BaseExperience: int
        ExperienceMultiplier: float
        MaxLevel: int
    }

    type LevelInfo = {
        Level: int
        Tier: string
        ExperienceRequired: int
        CumulativeExperience: int
    }

    // Calculate experience required for a specific level
    let calculateExperienceForLevel (level: int) (config: LevelConfig) : int =
        if level <= 0 then 0
        elif level >= config.MaxLevel then -1  // Max level reached
        else
            let levelFloat = float level
            int (float config.BaseExperience * Math.Pow(levelFloat, config.ExperienceMultiplier))

    // Calculate cumulative experience required for a level
    let calculateCumulativeExperience (level: int) (config: LevelConfig) : int =
        [1..level]
        |> List.sumBy (fun l -> calculateExperienceForLevel l config)

    // Determine tier based on level
    let determineTier (level: int) : string =
        match level with
        | _ when level >= 50 -> "Legendary"
        | _ when level >= 40 -> "Diamond"
        | _ when level >= 30 -> "Platinum"
        | _ when level >= 20 -> "Gold"
        | _ when level >= 10 -> "Silver"
        | _ -> "Bronze"

    // Get level info
    let getLevelInfo (level: int) (config: LevelConfig) : LevelInfo =
        {
            Level = level
            Tier = determineTier level
            ExperienceRequired = calculateExperienceForLevel level config
            CumulativeExperience = calculateCumulativeExperience level config
        }

    // Get level info using AI for intelligent level analysis
    let getLevelInfoWithAI (aiService: IAIService) (level: int) (config: LevelConfig) (learningContext: string) : Async<LevelInfo> =
        async {
            let prompt = sprintf "Analyze level progression: level %d, base XP %d, multiplier %.1f, max level %d, context '%s'. Return JSON: {\"tier\": \"Legendary/Diamond/Platinum/Gold/Silver/Bronze\", \"experienceRequired\": number, \"cumulativeExperience\": number}" level config.BaseExperience config.ExperienceMultiplier config.MaxLevel learningContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "education") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let tier = try root.GetProperty("tier").GetString() with _ -> determineTier level
            let experienceRequired = try root.GetProperty("experienceRequired").GetInt32() with _ -> calculateExperienceForLevel level config
            let cumulativeExperience = try root.GetProperty("cumulativeExperience").GetInt32() with _ -> calculateCumulativeExperience level config
            
            return {
                Level = level
                Tier = tier
                ExperienceRequired = experienceRequired
                CumulativeExperience = cumulativeExperience
            }
        }

// Experience Calculator
module ExperienceCalculator =

    type ExperienceGain = {
        ActivityType: string
        BaseExperience: int
        Multiplier: float
    }

    type ExperienceResult = {
        GainedExperience: int
        NewLevel: int
        Tier: string
        ProgressToNext: float
    }

    // Calculate experience gain based on activity
    let calculateExperienceGain (activity: string) (currentLevel: int) : int =
        let baseExperience = 
            match activity with
            | "course_completion" -> 100
            | "quiz_pass" -> 50
            | "assignment_submit" -> 30
            | "forum_post" -> 20
            | "daily_login" -> 10
            | "peer_review" -> 25
            | "mentorship" -> 75
            | _ -> 0

        // Level-based multiplier (higher levels get slightly less)
        let levelMultiplier = 
            if currentLevel > 30 then 0.8
            elif currentLevel > 20 then 0.9
            elif currentLevel > 10 then 1.0
            else 1.2

        int (float baseExperience * levelMultiplier)

    // Calculate level progression
    let calculateProgression (currentExperience: int) (currentLevel: int) (gainedExperience: int) (config: LevelProgressionCalculator.LevelConfig) : ExperienceResult =
        let totalExperience = currentExperience + gainedExperience
        let mutable newLevel = currentLevel
        let mutable remainingExperience = totalExperience
        let mutable maxLevelReached = false
        
        // Check for level ups
        while newLevel < config.MaxLevel && not maxLevelReached do
            let expNeeded = LevelProgressionCalculator.calculateExperienceForLevel (newLevel + 1) config
            if expNeeded = -1 then
                // Max level reached
                newLevel <- config.MaxLevel
                maxLevelReached <- true
            elif remainingExperience >= expNeeded then
                remainingExperience <- remainingExperience - expNeeded
                newLevel <- newLevel + 1
            else
                maxLevelReached <- true
        
        let tier = LevelProgressionCalculator.determineTier newLevel
        let expToNext = LevelProgressionCalculator.calculateExperienceForLevel (newLevel + 1) config
        let progressToNext = 
            if expToNext = -1 then 100.0
            elif expToNext <= 0 then 0.0
            else float remainingExperience / float expToNext * 100.0

        {
            GainedExperience = gainedExperience
            NewLevel = newLevel
            Tier = tier
            ProgressToNext = progressToNext
        }

    // Calculate progression using AI for intelligent experience analysis
    let calculateProgressionWithAI (aiService: IAIService) (currentExperience: int) (currentLevel: int) (gainedExperience: int) (config: LevelProgressionCalculator.LevelConfig) (activityContext: string) : Async<ExperienceResult> =
        async {
            let prompt = sprintf "Calculate level progression: current XP %d, level %d, gained %d, max level %d, context '%s'. Return JSON: {\"newLevel\": number, \"progressToNext\": number (0-100), \"tier\": \"Legendary/Diamond/Platinum/Gold/Silver/Bronze\"}" currentExperience currentLevel gainedExperience config.MaxLevel activityContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "education") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let newLevel = try root.GetProperty("newLevel").GetInt32() with _ -> currentLevel
            let progressToNext = try root.GetProperty("progressToNext").GetDouble() with _ -> 0.0
            let tier = try root.GetProperty("tier").GetString() with _ -> LevelProgressionCalculator.determineTier currentLevel
            
            return {
                GainedExperience = gainedExperience
                NewLevel = newLevel
                Tier = tier
                ProgressToNext = progressToNext
            }
        }

// Achievement Unlocker
module AchievementUnlocker =

    type Achievement = {
        Id: Guid
        Name: string
        Description: string
        Requirement: string
        ExperienceThreshold: int
        TierRequirement: string option
    }

    type UnlockResult = {
        AchievementId: Guid
        Unlocked: bool
        Reason: string
    }

    // Check if achievement can be unlocked
    let checkUnlockStatus (achievement: Achievement) (currentLevel: int) (currentTier: string) (totalExperience: int) : UnlockResult =
        let tierMatch = 
            match achievement.TierRequirement with
            | Some requiredTier -> requiredTier = currentTier
            | None -> true
        
        let levelMatch = currentLevel >= achievement.ExperienceThreshold
        
        if tierMatch && levelMatch then
            {
                AchievementId = achievement.Id
                Unlocked = true
                Reason = "All requirements met"
            }
        else
            let reasons = ResizeArray<string>()
            if not tierMatch then reasons.Add("Tier requirement not met")
            if not levelMatch then reasons.Add("Experience threshold not met")
            
            {
                AchievementId = achievement.Id
                Unlocked = false
                Reason = String.concat ", " reasons
            }

    // Get unlockable achievements
    let getUnlockableAchievements (achievements: Achievement list) (currentLevel: int) (currentTier: string) (totalExperience: int) : Achievement list =
        achievements
        |> List.filter (fun a ->
            let result = checkUnlockStatus a currentLevel currentTier totalExperience
            result.Unlocked)

    // Check unlock status using AI for intelligent achievement analysis
    let checkUnlockStatusWithAI (aiService: IAIService) (achievement: Achievement) (currentLevel: int) (currentTier: string) (totalExperience: int) (achievementContext: string) : Async<UnlockResult> =
        async {
            let prompt = sprintf "Check achievement unlock: '%s', requires tier %O, XP threshold %d, current level %d, tier '%s', total XP %d, context '%s'. Return JSON: {\"unlocked\": bool, \"reason\": string}" achievement.Name achievement.TierRequirement achievement.ExperienceThreshold currentLevel currentTier totalExperience achievementContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "education") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let tierMatch = match achievement.TierRequirement with | Some requiredTier -> requiredTier = currentTier | None -> true
            let levelMatch = currentLevel >= achievement.ExperienceThreshold
            let calcUnlocked() = tierMatch && levelMatch
            let unlocked = try root.GetProperty("unlocked").GetBoolean() with _ -> calcUnlocked()
            
            let calcReason() = 
                if unlocked then "All requirements met"
                else
                    let reasons = ResizeArray<string>()
                    if not tierMatch then reasons.Add("Tier requirement not met")
                    if not levelMatch then reasons.Add("Experience threshold not met")
                    String.concat ", " reasons
            let reason = try root.GetProperty("reason").GetString() with _ -> calcReason()
            
            return {
                AchievementId = achievement.Id
                Unlocked = unlocked
                Reason = reason
            }
        }
