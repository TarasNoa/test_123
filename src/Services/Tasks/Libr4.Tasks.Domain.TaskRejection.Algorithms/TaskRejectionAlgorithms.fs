namespace Libr4.Tasks.Domain.TaskRejection.Algorithms

open System
open System.Text.Json
open Libr4.Tasks.Domain.TaskRejection
open Libr4.AI.Infrastructure.AI

// Rejection Analyzer
module RejectionAnalyzer =

    type RejectionAnalysis = {
        PrimaryReason: string
        SecondaryReasons: string list
        Severity: string // Low, Medium, High
        ImprovementSuggestions: string list
    }

    // Analyze rejection reasons and provide insights using AI
    let analyzeRejection (aiService: IAIService) (rejectionCategory: string) (freelancerSkills: string list) (taskSkills: string list) (freelancerRate: int) (taskBudget: int) : Async<RejectionAnalysis> =
        async {
            let skillMatch = 
                taskSkills
                |> List.filter (fun ts -> freelancerSkills |> List.exists (fun fs -> fs.ToLower().Contains(ts.ToLower())))
                |> List.length
            
            let skillMatchRate = 
                if taskSkills.IsEmpty then 1f
                else float32 skillMatch / float32 taskSkills.Length
            
            let primaryReason = 
                match rejectionCategory with
                | "SkillsMismatch" -> "Skills do not match task requirements"
                | "Budget" -> "Rate exceeds task budget"
                | "Availability" -> "Freelancer not available for required timeline"
                | _ -> "Other reason"
            
            let skillsText = freelancerSkills |> String.concat ", "
            let taskSkillsText = taskSkills |> String.concat ", "
            let prompt = sprintf "Analyze rejection: category '%s', skills [%s] vs [%s], rate $%d vs budget $%d. Return JSON: {\"improvementSuggestions\": [string, string, string], \"severity\": \"Low/Medium/High\"}" rejectionCategory skillsText taskSkillsText freelancerRate taskBudget
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "rejection") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let improvementSuggestions = 
                try
                    root.GetProperty("improvementSuggestions").EnumerateArray()
                    |> Seq.map (fun s -> s.GetString())
                    |> List.ofSeq
                with _ ->
                    match rejectionCategory with
                    | "SkillsMismatch" -> 
                        if skillMatchRate < 0.5f then
                            ["Focus on developing required skills"; "Take courses in key areas"; "Build portfolio with relevant projects"]
                        else ["Highlight relevant experience"; "Update skill profile"]
                    | "Budget" -> ["Consider adjusting rate"; "Demonstrate value to justify higher rate"; "Offer different pricing options"]
                    | "Availability" -> ["Improve time management"; "Clearer communication about availability"; "Consider prioritizing tasks"]
                    | _ -> ["Request specific feedback"; "Improve overall profile presentation"]
            
            let severity = 
                try root.GetProperty("severity").GetString()
                with _ ->
                    if skillMatchRate < 0.3f then "High"
                    elif skillMatchRate < 0.6f then "Medium"
                    else "Low"
            
            let secondaryReasons = 
                if skillMatchRate < 0.5f then ["Insufficient skills match"]
                elif freelancerRate > taskBudget then ["Rate above budget"]
                else []
            
            return {
                PrimaryReason = primaryReason
                SecondaryReasons = secondaryReasons
                Severity = severity
                ImprovementSuggestions = improvementSuggestions
            }
        }

// Feedback Generator
module FeedbackGenerator =

    type FeedbackTemplate = {
        ProfessionalFeedback: string
        ConstructiveCriticism: string
        Encouragement: string
    }

    // Generate constructive feedback for rejected applicants using AI
    let generateFeedback (aiService: IAIService) (rejectionCategory: string) (freelancerName: string) (taskTitle: string) : Async<FeedbackTemplate> =
        async {
            let prompt = sprintf "Generate professional rejection feedback for '%s' applying to '%s' with reason '%s'. Return JSON: {\"professionalFeedback\": string, \"constructiveCriticism\": string}" freelancerName taskTitle rejectionCategory
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "feedback") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let professionalFeedback = 
                try root.GetProperty("professionalFeedback").GetString()
                with _ ->
                    match rejectionCategory with
                    | "SkillsMismatch" -> 
                        sprintf "Thank you for your interest in '%s'. After reviewing your profile, we've decided to move forward with other candidates whose skills more closely match our current requirements." taskTitle
                    | "Budget" -> 
                        sprintf "Thank you for applying to '%s'. While we appreciate your qualifications, your proposed rate exceeds our current budget for this project." taskTitle
                    | "Availability" -> 
                        sprintf "Thank you for your interest in '%s'. Unfortunately, your availability doesn't align with our project timeline." taskTitle
                    | _ -> 
                        sprintf "Thank you for applying to '%s'. We've decided to pursue other candidates for this position." taskTitle
            
            let constructiveCriticism = 
                try root.GetProperty("constructiveCriticism").GetString()
                with _ ->
                    match rejectionCategory with
                    | "SkillsMismatch" -> "Consider expanding your skill set in the areas most relevant to this type of work."
                    | "Budget" -> "You may want to review your pricing strategy or consider projects with different budget ranges."
                    | "Availability" -> "Improving your response time and availability communication could help with future opportunities."
                    | _ -> "We encourage you to continue applying for positions that match your qualifications."
            
            let encouragement = 
                "We value your interest and encourage you to apply for future opportunities that better match your skills and availability."
            
            return {
                ProfessionalFeedback = professionalFeedback
                ConstructiveCriticism = constructiveCriticism
                Encouragement = encouragement
            }
        }

// Rejection Trends
module RejectionTrends =

    type TrendMetrics = {
        TotalRejections: int
        ByCategory: Map<string, int>
        TopReason: string
        TrendDirection: string // Increasing, Decreasing, Stable
    }

    // Track rejection trends across tasks
    let trackTrends (rejections: (string * DateTimeOffset) list) : TrendMetrics =
        let totalRejections = rejections.Length
        
        let byCategory = 
            rejections
            |> List.groupBy fst
            |> List.map (fun (cat, items) -> (cat, items.Length))
            |> Map.ofList
        
        let topReason = 
            if byCategory.IsEmpty then "No data"
            else 
                byCategory
                |> Map.toSeq
                |> Seq.maxBy snd
                |> fst
        
        let recentRejections = 
            let weekAgo = DateTimeOffset.UtcNow.AddDays(-7.0)
            rejections |> List.filter (fun (_, date) -> date >= weekAgo) |> List.length
        
        let trendDirection = 
            let avgPerWeek = float32 totalRejections / 4f
            if float32 recentRejections > avgPerWeek * 1.5f then "Increasing"
            elif float32 recentRejections < avgPerWeek * 0.5f then "Decreasing"
            else "Stable"
        
        {
            TotalRejections = totalRejections
            ByCategory = byCategory
            TopReason = topReason
            TrendDirection = trendDirection
        }
