namespace Libr4.CRM.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Application.Abstractions

// Role Hierarchy Manager
module RoleHierarchyManager =

    type Role = {
        Name: string
        Level: int
        Permissions: string list
    }

    type HierarchyCheck = {
        IsHigher: bool
        LevelDifference: int
        CanGrant: bool
    }

    // Check role hierarchy
    let checkHierarchy (userRole: Role) (targetRole: Role) : HierarchyCheck =
        let isHigher = userRole.Level > targetRole.Level
        let levelDifference = userRole.Level - targetRole.Level
        let canGrant = isHigher && levelDifference >= 1
        
        {
            IsHigher = isHigher
            LevelDifference = levelDifference
            CanGrant = canGrant
        }

// Permission Checker
module PermissionChecker =

    type Permission = {
        Name: string
        Resource: string
        Action: string
    }

    type PermissionCheck = {
        Allowed: bool
        Reason: string
    }

    // Check if user has permission
    let checkPermission (userPermissions: Permission list) (requiredPermission: Permission) : PermissionCheck =
        let hasExactPermission = 
            userPermissions
            |> List.exists (fun p -> 
                p.Name = requiredPermission.Name &&
                p.Resource = requiredPermission.Resource &&
                p.Action = requiredPermission.Action)
        
        if hasExactPermission then
            {
                Allowed = true
                Reason = "Exact permission match"
            }
        else
            let hasResourcePermission = 
                userPermissions
                |> List.exists (fun p -> 
                    p.Resource = requiredPermission.Resource &&
                    p.Action = "*")
            
            if hasResourcePermission then
                {
                    Allowed = true
                    Reason = "Wildcard permission match"
                }
            else
                {
                    Allowed = false
                    Reason = "Permission not granted"
                }

// User Activity Analyzer
module UserActivityAnalyzer =

    type ActivityEvent = {
        Timestamp: DateTime
        Type: string
        Duration: TimeSpan option
    }

    type ActivitySummary = {
        TotalEvents: int
        ActiveDays: int
        AverageDailyEvents: float
        MostActiveHour: int
        LastActivity: DateTime
        ActivityTrend: string
    }

    // Analyze user activity
    let analyzeActivity (events: ActivityEvent list) : ActivitySummary =
        if events.IsEmpty then
            {
                TotalEvents = 0
                ActiveDays = 0
                AverageDailyEvents = 0.0
                MostActiveHour = 0
                LastActivity = DateTime.MinValue
                ActivityTrend = "No activity data"
            }
        else
            let totalEvents = List.length events
            let lastActivity = events |> List.maxBy (fun e -> e.Timestamp) |> fun e -> e.Timestamp
            
            let activeDays = 
                events
                |> List.map (fun e -> e.Timestamp.Date)
                |> List.distinct
                |> List.length
            
            let averageDailyEvents = 
                if activeDays > 0 then float totalEvents / float activeDays
                else 0.0
            
            let hourCounts = 
                events
                |> List.groupBy (fun e -> e.Timestamp.Hour)
                |> List.map (fun (hour, items) -> (hour, List.length items))
            
            let mostActiveHour = 
                if hourCounts.IsEmpty then 0
                else hourCounts |> List.maxBy snd |> fst
            
            let recentEvents = 
                events
                |> List.filter (fun e -> (DateTime.UtcNow - e.Timestamp).Days <= 7)
            
            let activityTrend = 
                if recentEvents.IsEmpty then "Inactive"
                elif List.length recentEvents > totalEvents / 2 then "Increasing"
                elif List.length recentEvents > totalEvents / 4 then "Stable"
                else "Decreasing"
            
            {
                TotalEvents = totalEvents
                ActiveDays = activeDays
                AverageDailyEvents = averageDailyEvents
                MostActiveHour = mostActiveHour
                LastActivity = lastActivity
                ActivityTrend = activityTrend
            }

    // Analyze activity using AI for intelligent assessment
    let analyzeActivityWithAI (aiService: IAIService) (events: ActivityEvent list) (userContext: string) : Async<ActivitySummary> =
        async {
            let eventsText = events |> List.map (fun e -> sprintf "%s at %s (dur: %s)" e.Type (e.Timestamp.ToString("o")) (match e.Duration with | Some d -> d.ToString() | None -> "N/A")) |> String.concat "; "
            
            let prompt = sprintf "Analyze user activity: events [%s], context '%s'. Return JSON: {\"activityTrend\": \"Increasing/Stable/Decreasing/Inactive\", \"mostActiveHour\": number (0-23)}" eventsText userContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "crm") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let calcActivityTrend() = 
                if events.IsEmpty then "No activity data"
                else
                    let recentEvents = events |> List.filter (fun e -> (DateTime.UtcNow - e.Timestamp).Days <= 7)
                    let totalEvents = events.Length
                    if recentEvents.IsEmpty then "Inactive"
                    elif recentEvents.Length > totalEvents / 2 then "Increasing"
                    elif recentEvents.Length > totalEvents / 4 then "Stable"
                    else "Decreasing"
            let activityTrend = try root.GetProperty("activityTrend").GetString() with _ -> calcActivityTrend()
            
            let calcMostActiveHour() = 
                if events.IsEmpty then 0
                else
                    let hourCounts = events |> List.groupBy (fun e -> e.Timestamp.Hour) |> List.map (fun (hour, items) -> (hour, List.length items))
                    if hourCounts.IsEmpty then 0 else hourCounts |> List.maxBy snd |> fst
            let mostActiveHour = try root.GetProperty("mostActiveHour").GetInt32() with _ -> calcMostActiveHour()
            
            let totalEvents = events.Length
            let activeDays = if events.IsEmpty then 0 else events |> List.map (fun e -> e.Timestamp.Date) |> List.distinct |> List.length
            let averageDailyEvents = if activeDays > 0 then float totalEvents / float activeDays else 0.0
            let lastActivity = if events.IsEmpty then DateTime.MinValue else events |> List.maxBy (fun e -> e.Timestamp) |> fun e -> e.Timestamp
            
            return {
                TotalEvents = totalEvents
                ActiveDays = activeDays
                AverageDailyEvents = averageDailyEvents
                MostActiveHour = mostActiveHour
                LastActivity = lastActivity
                ActivityTrend = activityTrend
            }
        }

// User Risk Assessor
module UserRiskAssessor =

    type UserMetrics = {
        LoginAttempts: int
        FailedAttempts: int
        SuspiciousActivities: int
        AccountAge: TimeSpan
        LastPasswordChange: DateTime
    }

    type RiskAssessment = {
        RiskLevel: string
        RiskScore: float
        Recommendations: string list
    }

    // Assess user risk level
    let assessRisk (metrics: UserMetrics) : RiskAssessment =
        let failedLoginRatio = 
            if metrics.LoginAttempts > 0 then float metrics.FailedAttempts / float metrics.LoginAttempts
            else 0.0
        
        let riskScore = 
            failedLoginRatio * 40.0 +
            float metrics.SuspiciousActivities * 20.0 +
            (if metrics.AccountAge.TotalDays < 30.0 then 30.0 else 0.0) +
            (if (DateTime.UtcNow - metrics.LastPasswordChange).TotalDays > 90.0 then 10.0 else 0.0)
        
        let riskLevel = 
            match riskScore with
            | _ when riskScore >= 80.0 -> "Critical"
            | _ when riskScore >= 60.0 -> "High"
            | _ when riskScore >= 40.0 -> "Medium"
            | _ when riskScore >= 20.0 -> "Low"
            | _ -> "Minimal"
        
        let recommendations = ResizeArray<string>()
        
        if riskScore >= 60.0 then recommendations.Add("Consider requiring additional authentication")
        if failedLoginRatio > 0.3 then recommendations.Add("Review recent login attempts")
        if metrics.SuspiciousActivities > 5 then recommendations.Add("Investigate suspicious activities")
        if (DateTime.UtcNow - metrics.LastPasswordChange).TotalDays > 90.0 then recommendations.Add("Prompt user to change password")
        if metrics.AccountAge.TotalDays < 30.0 then recommendations.Add("Monitor new account activity")
        
        {
            RiskLevel = riskLevel
            RiskScore = riskScore
            Recommendations = List.ofSeq recommendations
        }

    // Assess risk using AI for intelligent risk evaluation
    let assessRiskWithAI (aiService: IAIService) (metrics: UserMetrics) (securityContext: string) : Async<RiskAssessment> =
        async {
            let metricsText = sprintf "Logins:%d/%d failed, Suspicious:%d, AccountAge:%.0f days, LastPwdChange:%s" metrics.LoginAttempts metrics.FailedAttempts metrics.SuspiciousActivities metrics.AccountAge.TotalDays (metrics.LastPasswordChange.ToString("o"))
            
            let prompt = sprintf "Assess user risk: metrics [%s], context '%s'. Return JSON: {\"riskLevel\": \"Critical/High/Medium/Low/Minimal\", \"riskScore\": number (0-100), \"recommendations\": [string]}" metricsText securityContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "crm") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let failedLoginRatio = if metrics.LoginAttempts > 0 then float metrics.FailedAttempts / float metrics.LoginAttempts else 0.0
            let calcRiskScore() = failedLoginRatio * 40.0 + float metrics.SuspiciousActivities * 20.0 + (if metrics.AccountAge.TotalDays < 30.0 then 30.0 else 0.0) + (if (DateTime.UtcNow - metrics.LastPasswordChange).TotalDays > 90.0 then 10.0 else 0.0)
            
            let calcRiskLevel() = 
                let riskScore = calcRiskScore()
                if riskScore >= 80.0 then "Critical"
                elif riskScore >= 60.0 then "High"
                elif riskScore >= 40.0 then "Medium"
                elif riskScore >= 20.0 then "Low"
                else "Minimal"
            let riskLevel = try root.GetProperty("riskLevel").GetString() with _ -> calcRiskLevel()
            
            let riskScore = try root.GetProperty("riskScore").GetDouble() with _ -> calcRiskScore()
            
            let recommendations = 
                try
                    root.GetProperty("recommendations").EnumerateArray()
                    |> Seq.map (fun r -> r.GetString())
                    |> List.ofSeq
                with _ ->
                    let fallbackRecommendations = ResizeArray<string>()
                    let failedLoginRatio = if metrics.LoginAttempts > 0 then float metrics.FailedAttempts / float metrics.LoginAttempts else 0.0
                    if riskScore >= 60.0 then fallbackRecommendations.Add("Consider requiring additional authentication")
                    if failedLoginRatio > 0.3 then fallbackRecommendations.Add("Review recent login attempts")
                    if metrics.SuspiciousActivities > 5 then fallbackRecommendations.Add("Investigate suspicious activities")
                    if (DateTime.UtcNow - metrics.LastPasswordChange).TotalDays > 90.0 then fallbackRecommendations.Add("Prompt user to change password")
                    if metrics.AccountAge.TotalDays < 30.0 then fallbackRecommendations.Add("Monitor new account activity")
                    List.ofSeq fallbackRecommendations
            
            return {
                RiskLevel = riskLevel
                RiskScore = riskScore
                Recommendations = recommendations
            }
        }
