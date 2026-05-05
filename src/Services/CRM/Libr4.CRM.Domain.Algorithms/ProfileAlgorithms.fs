namespace Libr4.CRM.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Infrastructure.AI

// Profile Completeness Calculator
module ProfileCompletenessCalculator =

    type ProfileSection = {
        Name: string
        Weight: float
        IsComplete: bool
    }

    type CompletenessScore = {
        TotalScore: float
        Sections: ProfileSection list
        Suggestions: string list
    }

    // Calculate profile completeness
    let calculateCompleteness (hasDisplayName: bool) (hasBio: bool) (hasAvatar: bool) (hasLocation: bool) (hasSkills: bool) (hasExperience: bool) (hasEducation: bool) (hasSocialLinks: bool) : CompletenessScore =
        let sections = [
            { Name = "Display Name"; Weight = 0.15; IsComplete = hasDisplayName }
            { Name = "Bio"; Weight = 0.15; IsComplete = hasBio }
            { Name = "Avatar"; Weight = 0.10; IsComplete = hasAvatar }
            { Name = "Location"; Weight = 0.10; IsComplete = hasLocation }
            { Name = "Skills"; Weight = 0.20; IsComplete = hasSkills }
            { Name = "Experience"; Weight = 0.15; IsComplete = hasExperience }
            { Name = "Education"; Weight = 0.10; IsComplete = hasEducation }
            { Name = "Social Links"; Weight = 0.05; IsComplete = hasSocialLinks }
        ]
        
        let totalScore = sections |> List.sumBy (fun s -> if s.IsComplete then s.Weight else 0.0)
        
        let suggestions = 
            sections
            |> List.filter (fun s -> not s.IsComplete)
            |> List.map (fun s -> sprintf "Add your %s to improve your profile" s.Name)
        
        {
            TotalScore = totalScore
            Sections = sections
            Suggestions = suggestions
        }

    // Calculate completeness using AI for intelligent assessment
    let calculateCompletenessWithAI (aiService: IAIService) (hasDisplayName: bool) (hasBio: bool) (hasAvatar: bool) (hasLocation: bool) (hasSkills: bool) (hasExperience: bool) (hasEducation: bool) (hasSocialLinks: bool) (profileContext: string) : Async<CompletenessScore> =
        async {
            let sectionsText = sprintf "DisplayName:%b, Bio:%b, Avatar:%b, Location:%b, Skills:%b, Experience:%b, Education:%b, SocialLinks:%b" hasDisplayName hasBio hasAvatar hasLocation hasSkills hasExperience hasEducation hasSocialLinks
            
            let prompt = sprintf "Calculate profile completeness: sections [%s], context '%s'. Return JSON: {\"totalScore\": number (0-1), \"suggestions\": [string]}" sectionsText profileContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "crm") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let sections = [
                { Name = "Display Name"; Weight = 0.15; IsComplete = hasDisplayName }
                { Name = "Bio"; Weight = 0.15; IsComplete = hasBio }
                { Name = "Avatar"; Weight = 0.10; IsComplete = hasAvatar }
                { Name = "Location"; Weight = 0.10; IsComplete = hasLocation }
                { Name = "Skills"; Weight = 0.20; IsComplete = hasSkills }
                { Name = "Experience"; Weight = 0.15; IsComplete = hasExperience }
                { Name = "Education"; Weight = 0.10; IsComplete = hasEducation }
                { Name = "Social Links"; Weight = 0.05; IsComplete = hasSocialLinks }
            ]
            
            let totalScore = try root.GetProperty("totalScore").GetDouble() with _ -> sections |> List.sumBy (fun s -> if s.IsComplete then s.Weight else 0.0)
            
            let suggestions = 
                try
                    root.GetProperty("suggestions").EnumerateArray()
                    |> Seq.map (fun s -> s.GetString())
                    |> List.ofSeq
                with _ ->
                    sections |> List.filter (fun s -> not s.IsComplete) |> List.map (fun s -> sprintf "Add your %s to improve your profile" s.Name)

            
            return {
                TotalScore = totalScore
                Sections = sections
                Suggestions = suggestions
            }
        }

// Skill Matcher
module SkillMatcher =

    type SkillRequirement = {
        Name: string
        RequiredLevel: int
        Category: string
    }

    type UserSkill = {
        Name: string
        Level: int
        YearsOfExperience: int
        IsVerified: bool
    }

    type MatchResult = {
        SkillName: string
        MatchScore: float
        MeetsRequirement: bool
        Gap: int
    }

    // Match user skills against job requirements
    let matchSkills (requirements: SkillRequirement list) (userSkills: UserSkill list) : MatchResult list =
        requirements
        |> List.map (fun req ->
            let userSkill = userSkills |> List.tryFind (fun s -> s.Name = req.Name)
            
            match userSkill with
            | Some skill ->
                let gap = skill.Level - req.RequiredLevel
                let meetsRequirement = gap >= 0
                let matchScore = 
                    if meetsRequirement then 1.0
                    elif gap >= -1 then 0.7
                    elif gap >= -2 then 0.4
                    else 0.1
                
                {
                    SkillName = req.Name
                    MatchScore = matchScore
                    MeetsRequirement = meetsRequirement
                    Gap = gap
                }
            | None ->
                {
                    SkillName = req.Name
                    MatchScore = 0.0
                    MeetsRequirement = false
                    Gap = -req.RequiredLevel
                })

    // Match skills using AI for intelligent matching
    let matchSkillsWithAI (aiService: IAIService) (requirements: SkillRequirement list) (userSkills: UserSkill list) (jobContext: string) : Async<MatchResult list> =
        async {
            let requirementsText = requirements |> List.map (fun r -> sprintf "%s (level %d, cat %s)" r.Name r.RequiredLevel r.Category) |> String.concat "; "
            let userSkillsText = userSkills |> List.map (fun s -> sprintf "%s (level %d, exp %d, verified %b)" s.Name s.Level s.YearsOfExperience s.IsVerified) |> String.concat "; "
            
            let prompt = sprintf "Match skills: requirements [%s], user skills [%s], job context '%s'. Return JSON: {\"matches\": [{\"skillName\": string, \"matchScore\": number (0-1), \"meetsRequirement\": bool, \"gap\": number}]}" requirementsText userSkillsText jobContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "crm") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let matches = 
                try
                    root.GetProperty("matches").EnumerateArray()
                    |> Seq.map (fun m ->
                        {
                            SkillName = m.GetProperty("skillName").GetString()
                            MatchScore = m.GetProperty("matchScore").GetDouble()
                            MeetsRequirement = m.GetProperty("meetsRequirement").GetBoolean()
                            Gap = m.GetProperty("gap").GetInt32()
                        })
                    |> List.ofSeq
                with _ ->
                    matchSkills requirements userSkills
            
            return matches |> List.sortByDescending (fun m -> m.MatchScore)
        }

// Experience Analyzer
module ExperienceAnalyzer =

    type ExperienceEntry = {
        Company: string
        Position: string
        StartDate: DateTime
        EndDate: DateTime option
        IsCurrent: bool
    }

    type CareerSummary = {
        TotalYears: float
        CompaniesCount: int
        PositionsCount: int
        CurrentPosition: string option
        CareerProgression: string
    }

    // Analyze career experience
    let analyzeExperience (experiences: ExperienceEntry list) : CareerSummary =
        if experiences.IsEmpty then
            {
                TotalYears = 0.0
                CompaniesCount = 0
                PositionsCount = 0
                CurrentPosition = None
                CareerProgression = "No experience data"
            }
        else
            let sortedExperiences = experiences |> List.sortBy (fun e -> e.StartDate)
            
            let totalYears = 
                sortedExperiences
                |> List.sumBy (fun e ->
                    let endDate = e.EndDate |> Option.defaultValue DateTime.UtcNow
                    (endDate - e.StartDate).TotalDays / 365.0)
            
            let companiesCount = sortedExperiences |> List.map (fun e -> e.Company) |> List.distinct |> List.length
            let positionsCount = List.length sortedExperiences
            let currentPosition = sortedExperiences |> List.tryFind (fun e -> e.IsCurrent) |> Option.map (fun e -> e.Position)
            
            let careerProgression = 
                if totalYears < 1.0 then "Entry Level"
                elif totalYears < 3.0 then "Junior"
                elif totalYears < 5.0 then "Mid Level"
                elif totalYears < 8.0 then "Senior"
                else "Expert"
            
            {
                TotalYears = totalYears
                CompaniesCount = companiesCount
                PositionsCount = positionsCount
                CurrentPosition = currentPosition
                CareerProgression = careerProgression
            }

    // Analyze experience using AI for intelligent assessment
    let analyzeExperienceWithAI (aiService: IAIService) (experiences: ExperienceEntry list) (industryContext: string) : Async<CareerSummary> =
        async {
            let formatDate (d: DateTime option) = match d with | Some date -> date.ToString("o") | None -> "Present"
            let experiencesText = experiences |> List.map (fun e -> sprintf "%s at %s from %s to %s (current: %b)" e.Position e.Company (e.StartDate.ToString("o")) (formatDate e.EndDate) e.IsCurrent) |> String.concat "; "
            
            let prompt = sprintf "Analyze career experience: [%s], industry '%s'. Return JSON: {\"totalYears\": number, \"careerProgression\": \"Entry Level/Junior/Mid Level/Senior/Expert\"}" experiencesText industryContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "crm") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let sortedExperiences = if experiences.IsEmpty then [] else experiences |> List.sortBy (fun e -> e.StartDate)
            let calcTotalYears() = if experiences.IsEmpty then 0.0 else sortedExperiences |> List.sumBy (fun e -> let endDate = e.EndDate |> Option.defaultValue DateTime.UtcNow in (endDate - e.StartDate).TotalDays / 365.0)
            let totalYears = try root.GetProperty("totalYears").GetDouble() with _ -> calcTotalYears()
            
            let calcProgression() = if totalYears < 1.0 then "Entry Level" elif totalYears < 3.0 then "Junior" elif totalYears < 5.0 then "Mid Level" elif totalYears < 8.0 then "Senior" else "Expert"
            let careerProgression = try root.GetProperty("careerProgression").GetString() with _ -> calcProgression()
            
            let companiesCount = if experiences.IsEmpty then 0 else experiences |> List.map (fun e -> e.Company) |> List.distinct |> List.length
            let positionsCount = experiences.Length
            let currentPosition = if experiences.IsEmpty then None else experiences |> List.tryFind (fun e -> e.IsCurrent) |> Option.map (fun e -> e.Position)
            
            return {
                TotalYears = totalYears
                CompaniesCount = companiesCount
                PositionsCount = positionsCount
                CurrentPosition = currentPosition
                CareerProgression = careerProgression
            }
        }

// Profile Strength Calculator
module ProfileStrengthCalculator =

    type ProfileMetrics = {
        SkillCount: int
        VerifiedSkills: int
        ExperienceYears: float
        EducationCount: int
        CompletenessScore: float
    }

    type StrengthRating = {
        OverallScore: float
        Rating: string
        Strengths: string list
        Weaknesses: string list
    }

    // Calculate overall profile strength
    let calculateStrength (metrics: ProfileMetrics) : StrengthRating =
        let skillScore = float metrics.SkillCount / 10.0 |> min 1.0
        let verifiedScore = float metrics.VerifiedSkills / float metrics.SkillCount |> min 1.0
        let experienceScore = metrics.ExperienceYears / 10.0 |> min 1.0
        let educationScore = float metrics.EducationCount / 3.0 |> min 1.0
        let completenessScore = metrics.CompletenessScore
        
        let overallScore = 
            (skillScore * 0.3 + verifiedScore * 0.2 + experienceScore * 0.2 + educationScore * 0.1 + completenessScore * 0.2) * 100.0
        
        let rating = 
            match overallScore with
            | _ when overallScore >= 90.0 -> "Excellent"
            | _ when overallScore >= 75.0 -> "Strong"
            | _ when overallScore >= 60.0 -> "Good"
            | _ when overallScore >= 40.0 -> "Moderate"
            | _ -> "Needs Improvement"
        
        let strengths = ResizeArray<string>()
        let weaknesses = ResizeArray<string>()
        
        if skillScore >= 0.7 then strengths.Add("Strong skill set")
        else weaknesses.Add("Add more skills to your profile")
        
        if verifiedScore >= 0.5 then strengths.Add("Verified skills")
        else weaknesses.Add("Get your skills verified")
        
        if experienceScore >= 0.5 then strengths.Add("Solid experience")
        else weaknesses.Add("Add more work experience")
        
        if educationScore >= 0.5 then strengths.Add("Good educational background")
        elif metrics.EducationCount = 0 then weaknesses.Add("Add education details")
        
        if completenessScore >= 0.8 then strengths.Add("Complete profile")
        else weaknesses.Add("Complete your profile")
        
        {
            OverallScore = overallScore
            Rating = rating
            Strengths = List.ofSeq strengths
            Weaknesses = List.ofSeq weaknesses
        }

    // Calculate strength using AI for intelligent assessment
    let calculateStrengthWithAI (aiService: IAIService) (metrics: ProfileMetrics) (careerGoals: string) : Async<StrengthRating> =
        async {
            let metricsText = sprintf "Skills:%d/%d verified, Exp:%.1f years, Edu:%d, Completeness:%.2f" metrics.SkillCount metrics.VerifiedSkills metrics.ExperienceYears metrics.EducationCount metrics.CompletenessScore
            
            let prompt = sprintf "Calculate profile strength: metrics [%s], career goals '%s'. Return JSON: {\"overallScore\": number (0-100), \"rating\": \"Excellent/Strong/Good/Moderate/Needs Improvement\", \"strengths\": [string], \"weaknesses\": [string]}" metricsText careerGoals
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "crm") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let skillScore = float metrics.SkillCount / 10.0 |> min 1.0
            let verifiedScore = float metrics.VerifiedSkills / float metrics.SkillCount |> min 1.0
            let experienceScore = metrics.ExperienceYears / 10.0 |> min 1.0
            let educationScore = float metrics.EducationCount / 3.0 |> min 1.0
            let completenessScore = metrics.CompletenessScore
            
            let overallScore = try root.GetProperty("overallScore").GetDouble() with _ -> (skillScore * 0.3 + verifiedScore * 0.2 + experienceScore * 0.2 + educationScore * 0.1 + completenessScore * 0.2) * 100.0
            
            let calcRating() = if overallScore >= 90.0 then "Excellent" elif overallScore >= 75.0 then "Strong" elif overallScore >= 60.0 then "Good" elif overallScore >= 40.0 then "Moderate" else "Needs Improvement"
            let rating = try root.GetProperty("rating").GetString() with _ -> calcRating()
            
            let strengths = 
                try
                    root.GetProperty("strengths").EnumerateArray()
                    |> Seq.map (fun s -> s.GetString())
                    |> List.ofSeq
                with _ ->
                    let fallbackStrengths = ResizeArray<string>()
                    let skillScore = float metrics.SkillCount / 10.0 |> min 1.0
                    let verifiedScore = float metrics.VerifiedSkills / float metrics.SkillCount |> min 1.0
                    let experienceScore = metrics.ExperienceYears / 10.0 |> min 1.0
                    let educationScore = float metrics.EducationCount / 3.0 |> min 1.0
                    
                    if skillScore >= 0.7 then fallbackStrengths.Add("Strong skill set")
                    if verifiedScore >= 0.5 then fallbackStrengths.Add("Verified skills")
                    if experienceScore >= 0.5 then fallbackStrengths.Add("Solid experience")
                    if educationScore >= 0.5 then fallbackStrengths.Add("Good educational background")
                    if metrics.CompletenessScore >= 0.8 then fallbackStrengths.Add("Complete profile")
                    List.ofSeq fallbackStrengths
            
            let weaknesses = 
                try
                    root.GetProperty("weaknesses").EnumerateArray()
                    |> Seq.map (fun w -> w.GetString())
                    |> List.ofSeq
                with _ ->
                    let fallbackWeaknesses = ResizeArray<string>()
                    let skillScore = float metrics.SkillCount / 10.0 |> min 1.0
                    let verifiedScore = float metrics.VerifiedSkills / float metrics.SkillCount |> min 1.0
                    let experienceScore = metrics.ExperienceYears / 10.0 |> min 1.0
                    
                    if skillScore < 0.7 then fallbackWeaknesses.Add("Add more skills to your profile")
                    if verifiedScore < 0.5 then fallbackWeaknesses.Add("Get your skills verified")
                    if experienceScore < 0.5 then fallbackWeaknesses.Add("Add more work experience")
                    if metrics.EducationCount = 0 then fallbackWeaknesses.Add("Add education details")
                    if metrics.CompletenessScore < 0.8 then fallbackWeaknesses.Add("Complete your profile")
                    List.ofSeq fallbackWeaknesses
            
            return {
                OverallScore = overallScore
                Rating = rating
                Strengths = strengths
                Weaknesses = weaknesses
            }
        }
