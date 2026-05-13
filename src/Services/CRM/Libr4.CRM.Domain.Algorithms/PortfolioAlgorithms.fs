namespace Libr4.CRM.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Application.Abstractions

// Shared types for Portfolio algorithms
type ProjectSummary = {
    ProjectId: Guid
    Title: string
    Technologies: string list
    CompletedDate: DateTime option
    Duration: int option
}

// Portfolio Analytics
module PortfolioAnalytics =

    type PortfolioMetrics = {
        TotalProjects: int
        CompletedProjects: int
        TotalTechnologies: int
        UniqueTechnologies: int
        AverageProjectDuration: float
        CompletionRate: float
    }

    // Calculate portfolio metrics
    let calculateMetrics (projects: ProjectSummary list) : PortfolioMetrics =
        if projects.IsEmpty then
            {
                TotalProjects = 0
                CompletedProjects = 0
                TotalTechnologies = 0
                UniqueTechnologies = 0
                AverageProjectDuration = 0.0
                CompletionRate = 0.0
            }
        else
            let completedProjects = projects |> List.filter (fun p -> p.CompletedDate.IsSome)
            let allTechnologies = projects |> List.collect (fun p -> p.Technologies)
            let uniqueTechnologies = allTechnologies |> List.distinct
            
            let durations = 
                completedProjects
                |> List.choose (fun p -> p.Duration)
            
            let avgDuration = 
                if durations.IsEmpty then 0.0
                else float (List.sum durations) / float durations.Length
            
            {
                TotalProjects = List.length projects
                CompletedProjects = List.length completedProjects
                TotalTechnologies = List.length allTechnologies
                UniqueTechnologies = List.length uniqueTechnologies
                AverageProjectDuration = avgDuration
                CompletionRate = float (List.length completedProjects) / float (List.length projects)
            }

    // Calculate metrics using AI for deeper analysis
    let calculateMetricsWithAI (aiService: IAIService) (projects: ProjectSummary list) (careerContext: string) : Async<PortfolioMetrics> =
        async {
            let projectsText = projects |> List.map (fun p -> sprintf "Project: %s, Tech: %A, Completed: %s" p.Title p.Technologies (match p.CompletedDate with | Some d -> d.ToString("o") | None -> "No")) |> String.concat " | "
            
            let prompt = sprintf "Analyze portfolio metrics: projects [%s], context '%s'. Return JSON: {\"totalProjects\": number, \"completedProjects\": number, \"averageProjectDuration\": number, \"completionRate\": number (0-1)}" projectsText careerContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "crm") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let totalProjects = try root.GetProperty("totalProjects").GetInt32() with _ -> projects.Length
            let completedProjects = try root.GetProperty("completedProjects").GetInt32() with _ -> projects |> List.filter (fun p -> p.CompletedDate.IsSome) |> List.length
            let durations = projects |> List.choose (fun p -> p.Duration)
            let avgDuration = try root.GetProperty("averageProjectDuration").GetDouble() with _ -> if durations.IsEmpty then 0.0 else float (List.sum durations) / float durations.Length
            let completionRate = try root.GetProperty("completionRate").GetDouble() with _ -> if projects.IsEmpty then 0.0 else float completedProjects / float totalProjects
            
            let allTechnologies = projects |> List.collect (fun p -> p.Technologies)
            let uniqueTechnologies = allTechnologies |> List.distinct
            
            return {
                TotalProjects = totalProjects
                CompletedProjects = completedProjects
                TotalTechnologies = allTechnologies.Length
                UniqueTechnologies = uniqueTechnologies.Length
                AverageProjectDuration = avgDuration
                CompletionRate = completionRate
            }
        }

// Skill Extraction
module SkillExtractor =

    type TechnologySkill = {
        Name: string
        Count: int
        ProficiencyLevel: string
    }

    // Extract skills from portfolio projects
    let extractSkills (projects: ProjectSummary list) : TechnologySkill list =
        let technologyCounts = 
            projects
            |> List.collect (fun p -> p.Technologies)
            |> List.countBy id
            |> List.sortByDescending snd
        
        technologyCounts
        |> List.map (fun (tech, count) ->
            let proficiencyLevel = 
                if count >= 5 then "Expert"
                elif count >= 3 then "Advanced"
                elif count >= 1 then "Intermediate"
                else "Beginner"
            
            {
                Name = tech
                Count = count
                ProficiencyLevel = proficiencyLevel
            })

    // Extract skills using AI for intelligent assessment
    let extractSkillsWithAI (aiService: IAIService) (projects: ProjectSummary list) (industryContext: string) : Async<TechnologySkill list> =
        async {
            let projectsText = projects |> List.map (fun p -> sprintf "Project: %s, Tech: %A" p.Title p.Technologies) |> String.concat " | "
            
            let prompt = sprintf "Extract skills from portfolio: projects [%s], industry '%s'. Return JSON: {\"skills\": [{\"name\": string, \"proficiencyLevel\": \"Expert/Advanced/Intermediate/Beginner\", \"estimatedCount\": number}]}" projectsText industryContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "crm") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let skills = 
                try
                    root.GetProperty("skills").EnumerateArray()
                    |> Seq.map (fun s ->
                        {
                            Name = s.GetProperty("name").GetString()
                            Count = try s.GetProperty("estimatedCount").GetInt32() with _ -> 1
                            ProficiencyLevel = try s.GetProperty("proficiencyLevel").GetString() with _ -> "Intermediate"
                        })
                    |> List.ofSeq
                with _ ->
                    extractSkills projects
            
            return skills |> List.sortByDescending (fun s -> s.Count)
        }

// Portfolio Optimization
module PortfolioOptimizer =

    type OptimizationSuggestion = {
        Type: string
        Description: string
        Priority: string
    }

    // Analyze portfolio and provide optimization suggestions
    let analyzePortfolio (projects: ProjectSummary list) : OptimizationSuggestion list =
        let suggestions = ResizeArray<OptimizationSuggestion>()
        
        let incompleteProjects = projects |> List.filter (fun p -> p.CompletedDate.IsNone)
        
        if not incompleteProjects.IsEmpty then
            suggestions.Add({
                Type = "Completion"
                Description = sprintf "You have %d incomplete projects in your portfolio" incompleteProjects.Length
                Priority = if incompleteProjects.Length > 3 then "High" else "Medium"
            })
        
        let oldProjects = 
            projects
            |> List.filter (fun p -> 
                match p.CompletedDate with
                | Some date -> (DateTime.UtcNow - date).Days > 365
                | None -> false)
        
        if not oldProjects.IsEmpty then
            suggestions.Add({
                Type = "Update"
                Description = sprintf "Consider updating %d projects that are older than 1 year" oldProjects.Length
                Priority = "Low"
            })
        
        let technologyVariety = 
            projects
            |> List.collect (fun p -> p.Technologies)
            |> List.distinct
            |> List.length
        
        if technologyVariety < 3 && not projects.IsEmpty then
            suggestions.Add({
                Type = "Diversification"
                Description = "Consider adding projects with different technologies to showcase versatility"
                Priority = "Medium"
            })
        
        List.ofSeq suggestions

    // Analyze portfolio using AI for intelligent optimization suggestions
    let analyzePortfolioWithAI (aiService: IAIService) (projects: ProjectSummary list) (careerGoals: string) : Async<OptimizationSuggestion list> =
        async {
            let projectsText = projects |> List.map (fun p -> sprintf "Project: %s, Tech: %A, Completed: %s" p.Title p.Technologies (match p.CompletedDate with | Some d -> d.ToString("o") | None -> "No")) |> String.concat " | "
            
            let prompt = sprintf "Analyze portfolio for optimization: projects [%s], career goals '%s'. Return JSON: {\"suggestions\": [{\"type\": string, \"description\": string, \"priority\": \"High/Medium/Low\"}]}" projectsText careerGoals
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "crm") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let suggestions = 
                try
                    root.GetProperty("suggestions").EnumerateArray()
                    |> Seq.map (fun s ->
                        {
                            Type = s.GetProperty("type").GetString()
                            Description = s.GetProperty("description").GetString()
                            Priority = try s.GetProperty("priority").GetString() with _ -> "Medium"
                        })
                    |> List.ofSeq
                with _ ->
                    analyzePortfolio projects
            
            return suggestions |> List.sortBy (fun s -> match s.Priority with | "High" -> 0 | "Medium" -> 1 | _ -> 2)
        }
