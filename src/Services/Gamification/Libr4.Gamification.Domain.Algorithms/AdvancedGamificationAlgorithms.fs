namespace Libr4.Gamification.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Infrastructure.AI

// Challenge Progression Calculator
module ChallengeProgressionCalculator =

    type ChallengeProgress = {
        ChallengeId: Guid
        CurrentProgress: int
        TargetProgress: int
        Status: string
        PercentageComplete: float
        EstimatedCompletionDate: DateTime option
    }

    // Calculate challenge progression
    let calculateProgression (currentProgress: int) (targetProgress: int) (startDate: DateTime) (endDate: DateTime) : ChallengeProgress =
        let percentage = 
            if targetProgress <= 0 then 0.0
            else float currentProgress / float targetProgress * 100.0
        
        let status = 
            if currentProgress >= targetProgress then "Completed"
            elif currentProgress = 0 then "NotStarted"
            else "InProgress"
        
        let estimatedCompletion = 
            if percentage >= 100.0 then Some DateTime.UtcNow
            elif percentage <= 0.0 then None
            else
                let totalDuration = (endDate - startDate).TotalDays
                let elapsedDuration = (DateTime.UtcNow - startDate).TotalDays
                let progressRate = float currentProgress / elapsedDuration
                if progressRate > 0.0 then
                    let remainingProgress = targetProgress - currentProgress
                    let remainingDays = float remainingProgress / progressRate
                    Some (DateTime.UtcNow.AddDays(remainingDays))
                else
                    None
        
        {
            ChallengeId = Guid.Empty
            CurrentProgress = currentProgress
            TargetProgress = targetProgress
            Status = status
            PercentageComplete = percentage
            EstimatedCompletionDate = estimatedCompletion
        }

    // Calculate progression using AI for intelligent progress analysis
    let calculateProgressionWithAI (aiService: IAIService) (currentProgress: int) (targetProgress: int) (startDate: DateTime) (endDate: DateTime) (challengeContext: string) : Async<ChallengeProgress> =
        async {
            let prompt = sprintf "Analyze challenge progression: current %d, target %d, start %s, end %s, context '%s'. Return JSON: {\"status\": \"Completed/InProgress/NotStarted\", \"percentage\": number (0-100), \"estimatedDaysToComplete\": number}" currentProgress targetProgress (startDate.ToString("o")) (endDate.ToString("o")) challengeContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "gamification") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcStatus() = 
                if currentProgress >= targetProgress then "Completed"
                elif currentProgress = 0 then "NotStarted"
                else "InProgress"
            let status = try root.GetProperty("status").GetString() with _ -> calcStatus()
            
            let calcPercentage() = if targetProgress <= 0 then 0.0 else float currentProgress / float targetProgress * 100.0
            let percentage = try root.GetProperty("percentage").GetDouble() with _ -> calcPercentage()
            
            let estimatedCompletion = 
                try
                    let estimatedDays = root.GetProperty("estimatedDaysToComplete").GetDouble()
                    if estimatedDays > 0.0 then Some (DateTime.UtcNow.AddDays(estimatedDays)) else None
                with _ ->
                    if percentage >= 100.0 then Some DateTime.UtcNow
                    elif percentage <= 0.0 then None
                    else
                        let totalDuration = (endDate - startDate).TotalDays
                        let elapsedDuration = (DateTime.UtcNow - startDate).TotalDays
                        let progressRate = float currentProgress / elapsedDuration
                        if progressRate > 0.0 then
                            let remainingProgress = targetProgress - currentProgress
                            let remainingDays = float remainingProgress / progressRate
                            Some (DateTime.UtcNow.AddDays(remainingDays))
                        else None
            
            return {
                ChallengeId = Guid.Empty
                CurrentProgress = currentProgress
                TargetProgress = targetProgress
                Status = status
                PercentageComplete = percentage
                EstimatedCompletionDate = estimatedCompletion
            }
        }

// Leaderboard Ranking
module LeaderboardRanking =

    type LeaderboardEntry = {
        UserId: Guid
        Username: string
        Points: int
        Rank: int
        Tier: string
    }

    type RankingUpdate = {
        PreviousRank: int
        NewRank: int
        RankChange: int
        Trend: string  // "up", "down", "stable"
    }

    // Calculate leaderboard rankings
    let calculateRankings (entries: LeaderboardEntry list) : LeaderboardEntry list =
        let sorted = entries |> List.sortByDescending (fun e -> e.Points)
        
        sorted
        |> List.mapi (fun index entry ->
            let rank = index + 1
            { entry with Rank = rank })

    // Calculate tier based on points
    let calculateTier (points: int) : string =
        match points with
        | _ when points >= 10000 -> "Diamond"
        | _ when points >= 5000 -> "Platinum"
        | _ when points >= 2500 -> "Gold"
        | _ when points >= 1000 -> "Silver"
        | _ -> "Bronze"

    // Calculate ranking changes
    let calculateRankChange (previousRank: int) (newRank: int) : RankingUpdate =
        let rankChange = previousRank - newRank
        let trend = 
            if rankChange > 0 then "up"
            elif rankChange < 0 then "down"
            else "stable"
        
        {
            PreviousRank = previousRank
            NewRank = newRank
            RankChange = rankChange
            Trend = trend
        }

    // Calculate tier using AI for intelligent tier assignment
    let calculateTierWithAI (aiService: IAIService) (points: int) (rankingContext: string) : Async<string> =
        async {
            let prompt = sprintf "Determine player tier: %d points, context '%s'. Return JSON: {\"tier\": \"Diamond/Platinum/Gold/Silver/Bronze\"}" points rankingContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "gamification") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcTier() = 
                match points with
                | _ when points >= 10000 -> "Diamond"
                | _ when points >= 5000 -> "Platinum"
                | _ when points >= 2500 -> "Gold"
                | _ when points >= 1000 -> "Silver"
                | _ -> "Bronze"
            let tier = try root.GetProperty("tier").GetString() with _ -> calcTier()
            
            return tier
        }

// Reward Calculator
module RewardCalculator =

    type Reward = {
        Points: int
        Badge: string option
        Multiplier: float
    }

    type RewardConfig = {
        BasePoints: int
        DifficultyMultiplier: float
        SpeedBonus: float
        StreakBonus: float
    }

    // Calculate reward based on challenge completion
    let calculateReward (config: RewardConfig) (difficulty: int) (completionTime: TimeSpan) (targetTime: TimeSpan) (streak: int) : Reward =
        let difficultyMultiplier = float difficulty * config.DifficultyMultiplier
        
        let speedBonus = 
            if completionTime < targetTime then config.SpeedBonus
            else 0.0
        
        let streakBonus = float streak * config.StreakBonus
        
        let totalMultiplier = 1.0 + difficultyMultiplier + speedBonus + streakBonus
        let points = int (float config.BasePoints * totalMultiplier)
        
        let badge = 
            if totalMultiplier >= 3.0 then Some "Legendary"
            elif totalMultiplier >= 2.0 then Some "Epic"
            elif totalMultiplier >= 1.5 then Some "Rare"
            else None
        
        {
            Points = points
            Badge = badge
            Multiplier = totalMultiplier
        }

    // Calculate reward using AI for intelligent reward distribution
    let calculateRewardWithAI (aiService: IAIService) (config: RewardConfig) (difficulty: int) (completionTime: TimeSpan) (targetTime: TimeSpan) (streak: int) (rewardContext: string) : Async<Reward> =
        async {
            let prompt = sprintf "Calculate reward: base %d, difficulty %d, completion %.0f min, target %.0f min, streak %d, context '%s'. Return JSON: {\"points\": number, \"badge\": \"Legendary/Epic/Rare/null\", \"multiplier\": number}" config.BasePoints difficulty completionTime.TotalMinutes targetTime.TotalMinutes streak rewardContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "gamification") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let difficultyMultiplier = float difficulty * config.DifficultyMultiplier
            let speedBonus = if completionTime < targetTime then config.SpeedBonus else 0.0
            let streakBonus = float streak * config.StreakBonus
            let calcPoints() = 
                let totalMultiplier = 1.0 + difficultyMultiplier + speedBonus + streakBonus
                int (float config.BasePoints * totalMultiplier)
            let points = try root.GetProperty("points").GetInt32() with _ -> calcPoints()
            
            let badgeStr = try root.GetProperty("badge").GetString() with _ -> "null"
            let badge = 
                match badgeStr with
                | "Legendary" -> Some "Legendary"
                | "Epic" -> Some "Epic"
                | "Rare" -> Some "Rare"
                | _ -> None
            
            let difficultyMultiplier = float difficulty * config.DifficultyMultiplier
            let speedBonus = if completionTime < targetTime then config.SpeedBonus else 0.0
            let streakBonus = float streak * config.StreakBonus
            let calcMultiplier() = 1.0 + difficultyMultiplier + speedBonus + streakBonus
            let multiplier = try root.GetProperty("multiplier").GetDouble() with _ -> calcMultiplier()
            
            return {
                Points = points
                Badge = badge
                Multiplier = multiplier
            }
        }

// Challenge Generator
module ChallengeGenerator =

    type ChallengeTemplate = {
        Title: string
        Description: string
        Type: string
        Difficulty: int
        TargetProgress: int
        RewardPoints: int
    }

    type GeneratedChallenge = {
        Title: string
        Description: string
        Type: string
        Difficulty: int
        TargetProgress: int
        RewardPoints: int
        StartDate: DateTime
        EndDate: DateTime
    }

    // Generate challenge from template
    let generateChallenge (template: ChallengeTemplate) (startDate: DateTime) (durationDays: int) : GeneratedChallenge =
        let endDate = startDate.AddDays(float durationDays)
        
        {
            Title = template.Title
            Description = template.Description
            Type = template.Type
            Difficulty = template.Difficulty
            TargetProgress = template.TargetProgress
            RewardPoints = template.RewardPoints
            StartDate = startDate
            EndDate = endDate
        }

    // Generate daily challenge
    let generateDailyChallenge (templates: ChallengeTemplate list) (userLevel: int) : GeneratedChallenge =
        let suitableTemplates = 
            templates 
            |> List.filter (fun t -> t.Difficulty <= userLevel + 2)
        
        let template = 
            if suitableTemplates.IsEmpty then templates.[0]
            else 
                let random = System.Random()
                suitableTemplates.[random.Next(suitableTemplates.Length)]
        
        let startDate = DateTime.UtcNow
        generateChallenge template startDate 1

    // Generate challenge using AI for intelligent challenge creation
    let generateDailyChallengeWithAI (aiService: IAIService) (templates: ChallengeTemplate list) (userLevel: int) (challengeContext: string) : Async<GeneratedChallenge> =
        async {
            let templatesText = templates |> List.map (fun t -> sprintf "%s (diff %d, reward %d)" t.Title t.Difficulty t.RewardPoints) |> String.concat "; "
            
            let prompt = sprintf "Generate daily challenge: templates [%s], user level %d, context '%s'. Return JSON: {\"title\": string, \"description\": string, \"type\": string, \"difficulty\": number, \"targetProgress\": number, \"rewardPoints\": number}" templatesText userLevel challengeContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "gamification") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let suitableTemplates = templates |> List.filter (fun t -> t.Difficulty <= userLevel + 2)
            let selectTemplate() = 
                if suitableTemplates.IsEmpty then templates.[0]
                else
                    let random = System.Random()
                    suitableTemplates.[random.Next(suitableTemplates.Length)]
            let template = selectTemplate()
            
            let title = try root.GetProperty("title").GetString() with _ -> template.Title
            let description = try root.GetProperty("description").GetString() with _ -> template.Description
            let challengeType = try root.GetProperty("type").GetString() with _ -> template.Type
            let difficulty = try root.GetProperty("difficulty").GetInt32() with _ -> template.Difficulty
            let targetProgress = try root.GetProperty("targetProgress").GetInt32() with _ -> template.TargetProgress
            let rewardPoints = try root.GetProperty("rewardPoints").GetInt32() with _ -> template.RewardPoints
            
            let startDate = DateTime.UtcNow
            
            return {
                Title = title
                Description = description
                Type = challengeType
                Difficulty = difficulty
                TargetProgress = targetProgress
                RewardPoints = rewardPoints
                StartDate = startDate
                EndDate = startDate.AddDays(1.0)
            }
        }
