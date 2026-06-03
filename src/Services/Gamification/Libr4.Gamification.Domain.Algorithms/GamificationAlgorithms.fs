namespace Libr4.Gamification.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Application.Abstractions

// Simple types to avoid circular dependency
type Rarity = Common = 0 | Rare = 1 | Epic = 2 | Legendary = 3

// XP Calculation Algorithms
module XPSystem =

    // Calculate XP required for a specific level (exponential growth)
    let calculateXPForLevel (level: int) : int64 =
        // Formula: 1000 * 1.5^(level-1)
        1000L * int64 (Math.Pow(1.5, float (level - 1)))

    // Calculate XP required for a specific level using AI for dynamic scaling
    let calculateXPForLevelAI (aiService: IAIService) (level: int) (userEngagement: float) : Async<int64> =
        async {
            let prompt = sprintf "Calculate XP needed for level %d with user engagement %.1f. Return JSON: {\"xpRequired\": number}" level userEngagement
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "gamification") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let xpRequired = 
                try root.GetProperty("xpRequired").GetInt64()
                with _ -> calculateXPForLevel level
            
            return xpRequired
        }

    // Calculate total XP needed to reach a level from level 1
    let calculateTotalXPToLevel (level: int) : int64 =
        seq { 1 .. level }
        |> Seq.sumBy calculateXPForLevel

    // Calculate level from total XP
    let calculateLevelFromXP (totalXP: int64) : int =
        let rec findLevel (currentLevel: int) (accumulatedXP: int64) : int =
            let requiredXP = calculateXPForLevel currentLevel
            if accumulatedXP + requiredXP > totalXP then
                currentLevel - 1
            else
                findLevel (currentLevel + 1) (accumulatedXP + requiredXP)
        findLevel 1 0L

    // Calculate progress percentage to next level
    let calculateProgressToNextLevel (totalXP: int64) (currentLevel: int) : float32 =
        let xpForCurrentLevel = calculateTotalXPToLevel currentLevel
        let xpForNextLevel = calculateTotalXPToLevel (currentLevel + 1)
        let xpInCurrentLevel = totalXP - xpForCurrentLevel
        let xpNeededForNextLevel = xpForNextLevel - xpForCurrentLevel
        if xpNeededForNextLevel = 0L then 100.0f
        else float32 (xpInCurrentLevel * 100L / xpNeededForNextLevel)

// Achievement Criteria Evaluation
module AchievementCriteria =

    type AchievementCondition =
        | XPThreshold of int64
        | LevelThreshold of int
        | TaskCountThreshold of int
        | StreakDaysThreshold of int
        | CustomCondition of (string -> bool)

    // Evaluate if achievement criteria is met
    let evaluateCriteria condition (context: Map<string, obj>) =
        match condition with
        | XPThreshold threshold ->
            match context.TryFind "totalXP" with
            | Some xp ->
                match xp with
                | :? int64 as xpValue -> xpValue >= threshold
                | _ -> false
            | None -> false
        | LevelThreshold threshold ->
            match context.TryFind "level" with
            | Some level ->
                match level with
                | :? int as levelValue -> levelValue >= threshold
                | _ -> false
            | None -> false
        | TaskCountThreshold threshold ->
            match context.TryFind "completedTasks" with
            | Some tasks ->
                match tasks with
                | :? int as taskCount -> taskCount >= threshold
                | _ -> false
            | None -> false
        | StreakDaysThreshold threshold ->
            match context.TryFind "streakDays" with
            | Some streak ->
                match streak with
                | :? int as streakDays -> streakDays >= threshold
                | _ -> false
            | None -> false
        | CustomCondition evaluator ->
            evaluator "custom" // Simplified for now

    // Generate achievement suggestions using AI
    let generateAchievementSuggestions (aiService: IAIService) (userLevel: int) (userXP: int64) (completedTasks: int) : Async<string list> =
        async {
            let prompt = sprintf "Suggest achievements for user: level %d, XP %d, completed tasks %d. Return JSON: {\"achievements\": [string, string, string]}" userLevel userXP completedTasks
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "gamification") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let achievements = 
                try
                    root.GetProperty("achievements").EnumerateArray()
                    |> Seq.map (fun a -> a.GetString())
                    |> List.ofSeq
                with _ ->
                    [
                        "First Steps - Complete your first task"
                        "Rising Star - Reach level 5"
                        "Task Master - Complete 10 tasks"
                    ]
            
            return achievements
        }

// Leaderboard Ranking Algorithms
module Leaderboard =

    // Simple record type for leaderboard entries (to avoid circular dependency)
    type LeaderboardEntry = {
        UserId: Guid
        Score: int64
        Rank: int
    }

    // Calculate rank from score using dense ranking (no gaps)
    let calculateDenseRank (scores: (Guid * int64) list) (userId: Guid) : int =
        scores
        |> List.filter (fun (id, _) -> id = userId)
        |> List.map snd
        |> List.sortByDescending id
        |> List.findIndex ((=) (List.max scores |> snd))
        |> (+) 1

    // Calculate rank from score using standard ranking (with gaps)
    let calculateStandardRank (scores: (Guid * int64) list) (userId: Guid) : int =
        let userScore = scores |> List.find (fst >> (=) userId) |> snd
        scores
        |> List.filter (fun (_, score) -> score > userScore)
        |> List.length
        |> (+) 1

    // Recalculate all ranks after score update
    let recalculateRanks (entries: LeaderboardEntry list) : LeaderboardEntry list =
        entries
        |> List.sortByDescending (fun e -> e.Score)
        |> List.mapi (fun index entry ->
            { entry with Rank = index + 1 })

    // Predict leaderboard position change using AI
    let predictLeaderboardPosition (aiService: IAIService) (currentScore: int64) (targetScore: int64) (currentRank: int) (totalPlayers: int) : Async<int> =
        async {
            let prompt = sprintf "Predict leaderboard position: current score %d, target score %d, current rank %d, total players %d. Return JSON: {\"predictedRank\": number}" currentScore targetScore currentRank totalPlayers
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "gamification") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let predictedRank = 
                try root.GetProperty("predictedRank").GetInt32()
                with _ ->
                    let scoreDiff = targetScore - currentScore
                    let rankChange = scoreDiff / (currentScore / int64 currentRank |> max 1L) |> int
                    max 1 (currentRank - rankChange)
            
            return predictedRank
        }

// Streak Calculation Algorithms
module StreakSystem =

    // Calculate current streak from activity dates
    let calculateStreak (activityDates: DateTime list) : int =
        let sortedDates = activityDates |> List.sortByDescending id
        let today = DateTime.UtcNow.Date

        let rec countStreak (dates: DateTime list) (currentStreak: int) (consecutiveDays: int) : int =
            match dates with
            | [] -> currentStreak
            | date :: rest ->
                let daysDiff = (date.Date - today).Days
                if daysDiff = 0 then
                    countStreak rest currentStreak (consecutiveDays + 1)
                elif daysDiff = -consecutiveDays then
                    countStreak rest (currentStreak + 1) (consecutiveDays + 1)
                else
                    currentStreak

        countStreak sortedDates 0 0

    // Check if streak should be reset
    let shouldResetStreak (lastActivityDate: DateTime) : bool =
        let daysSinceLastActivity = (DateTime.UtcNow.Date - lastActivityDate.Date).Days
        daysSinceLastActivity > 1

    // Predict streak continuation using AI
    let predictStreakContinuation (aiService: IAIService) (currentStreak: int) (activityPattern: int list) : Async<float> =
        async {
            let patternText = activityPattern |> List.map string |> String.concat ", "
            let prompt = sprintf "Predict streak continuation probability: current streak %d days, activity pattern [%s]. Return JSON: {\"probability\": number (0-1)}" currentStreak patternText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "gamification") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let probability = 
                try root.GetProperty("probability").GetSingle()
                with _ ->
                    // Fallback: simple probability based on streak length
                    if currentStreak < 3 then 0.5f
                    elif currentStreak < 7 then 0.7f
                    elif currentStreak < 14 then 0.8f
                    else 0.9f
            
            return float probability
        }

// Reward Calculation Algorithms
module RewardSystem =

    // Calculate reward based on achievement rarity
    let calculateRewardByRarity (rarity: Rarity) : int64 =
        match rarity with
        | Rarity.Common -> 100L
        | Rarity.Rare -> 500L
        | Rarity.Epic -> 2000L
        | Rarity.Legendary -> 10000L

    // Calculate bonus multiplier based on streak
    let calculateStreakMultiplier (streakDays: int) : float32 =
        match streakDays with
        | days when days < 7 -> 1.0f
        | days when days < 14 -> 1.2f
        | days when days < 30 -> 1.5f
        | days when days < 60 -> 2.0f
        | _ -> 2.5f

    // Calculate total reward with streak bonus
    let calculateTotalReward (baseReward: int64) (streakDays: int) : int64 =
        let multiplier = calculateStreakMultiplier streakDays
        int64 (float baseReward * float multiplier)

    // Calculate dynamic reward using AI
    let calculateDynamicReward (aiService: IAIService) (achievementType: string) (userLevel: int) (streakDays: int) : Async<int64> =
        async {
            let prompt = sprintf "Calculate dynamic reward: achievement type '%s', user level %d, streak %d days. Return JSON: {\"reward\": number}" achievementType userLevel streakDays
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "gamification") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let reward = 
                try root.GetProperty("reward").GetInt64()
                with _ ->
                    let baseReward = calculateRewardByRarity Rarity.Common
                    let multiplier = calculateStreakMultiplier streakDays
                    int64 (float baseReward * float multiplier)
            
            return reward
        }
