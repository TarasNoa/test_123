namespace Libr4.AI.Domain.InterviewQuestions.Algorithms

open System
open System.Text.Json
open Libr4.AI.Domain.InterviewQuestions
open Libr4.AI.Infrastructure.AI

// Question Generator
module QuestionGenerator =

    type GeneratedQuestion = {
        Question: string
        Category: string
        Difficulty: string
        ExpectedAnswer: string option
    }

    // Generate interview questions based on job description using AI
    let generateQuestions (aiService: IAIService) (jobTitle: string) (jobDescription: string) : Async<GeneratedQuestion list> =
        async {
            let prompt = sprintf "Generate 5 interview questions for position '%s' with description: '%s'. Return JSON: {\"questions\": [{\"question\": string, \"category\": \"Technical/Behavioral/Situational\", \"difficulty\": \"Easy/Medium/Hard\", \"expectedAnswer\": string}]}" jobTitle jobDescription
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "interview") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let questions = 
                try
                    root.GetProperty("questions").EnumerateArray()
                    |> Seq.map (fun q ->
                        {
                            Question = q.GetProperty("question").GetString()
                            Category = q.GetProperty("category").GetString()
                            Difficulty = q.GetProperty("difficulty").GetString()
                            ExpectedAnswer = Some (q.GetProperty("expectedAnswer").GetString())
                        })
                    |> List.ofSeq
                with _ ->
                    // Fallback to heuristic-based questions
                    let descLower = jobDescription.ToLower()
                    let titleLower = jobTitle.ToLower()
                    
                    let technicalQuestions = [
                        {
                            Question = "Describe your experience with the primary technologies required for this role."
                            Category = "Technical"
                            Difficulty = "Medium"
                            ExpectedAnswer = Some "Candidate should discuss specific projects and technologies used"
                        }
                        {
                            Question = "How do you approach debugging complex technical issues?"
                            Category = "Technical"
                            Difficulty = "Medium"
                            ExpectedAnswer = Some "Should include systematic debugging methodology"
                        }
                        {
                            Question = "Explain a technical concept you've learned recently and how you applied it."
                            Category = "Technical"
                            Difficulty = "Hard"
                            ExpectedAnswer = Some "Demonstrates continuous learning and practical application"
                        }
                    ]
                    
                    let behavioralQuestions = [
                        {
                            Question = "Tell me about a time you had to work with a difficult team member."
                            Category = "Behavioral"
                            Difficulty = "Medium"
                            ExpectedAnswer = Some "Should show conflict resolution skills"
                        }
                        {
                            Question = "Describe a situation where you had to meet a tight deadline."
                            Category = "Behavioral"
                            Difficulty = "Medium"
                            ExpectedAnswer = Some "Should demonstrate time management and prioritization"
                        }
                        {
                            Question = "How do you handle constructive criticism?"
                            Category = "Behavioral"
                            Difficulty = "Easy"
                            ExpectedAnswer = Some "Should show growth mindset"
                        }
                    ]
                    
                    let situationalQuestions = [
                        {
                            Question = "What would you do if you discovered a critical bug just before a release?"
                            Category = "Situational"
                            Difficulty = "Hard"
                            ExpectedAnswer = Some "Should balance business needs with quality"
                        }
                        {
                            Question = "How would you handle a situation where requirements change mid-project?"
                            Category = "Situational"
                            Difficulty = "Medium"
                            ExpectedAnswer = Some "Should show adaptability and communication"
                        }
                    ]
                    
                    // Filter and prioritize questions based on job description
                    let allQuestions = technicalQuestions @ behavioralQuestions @ situationalQuestions
                    
                    if descLower.Contains("team") || descLower.Contains("collabor") then
                        behavioralQuestions @ technicalQuestions.[0..1]
                    elif descLower.Contains("technical") || descLower.Contains("develop") then
                        technicalQuestions @ behavioralQuestions.[0..1]
                    else
                        allQuestions
            
            return questions
        }

// Difficulty Assessor
module DifficultyAssessor =

    type DifficultyScore = {
        Score: int
        Level: string
    }

    // Assess difficulty of a question using AI
    let assessDifficulty (aiService: IAIService) (question: string) : Async<DifficultyScore> =
        async {
            let prompt = sprintf "Assess difficulty of interview question: '%s'. Return JSON: {\"score\": number (0-10), \"level\": \"Easy/Medium/Hard\"}" question
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "interview") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let score = 
                try root.GetProperty("score").GetInt32()
                with _ ->
                    let questionLower = question.ToLower()
                    let technicalTerms = ["debugging"; "architecture"; "optimization"; "scalability"; "security"]
                    let complexPhrases = ["complex"; "challenging"; "difficult"; "advanced"]
                    
                    let techCount = technicalTerms |> List.filter (fun t -> questionLower.Contains(t)) |> List.length
                    let complexCount = complexPhrases |> List.filter (fun p -> questionLower.Contains(p)) |> List.length
                    
                    techCount * 2 + complexCount * 3
            
            let level = 
                try root.GetProperty("level").GetString()
                with _ ->
                    if score <= 2 then "Easy"
                    elif score <= 5 then "Medium"
                    else "Hard"
            
            return {
                Score = score
                Level = level
            }
        }

// Question Categorizer
module QuestionCategorizer =

    type CategoryMatch = {
        Category: string
        Confidence: float32
    }

    // Categorize a question using AI
    let categorizeQuestion (aiService: IAIService) (question: string) : Async<CategoryMatch list> =
        async {
            let prompt = sprintf "Categorize interview question: '%s'. Return JSON: {\"categories\": [{\"category\": \"Technical/Behavioral/Situational\", \"confidence\": number (0-100)}]}" question
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "interview") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let categories = 
                try
                    root.GetProperty("categories").EnumerateArray()
                    |> Seq.map (fun c ->
                        {
                            Category = c.GetProperty("category").GetString()
                            Confidence = c.GetProperty("confidence").GetSingle()
                        })
                    |> List.ofSeq
                with _ ->
                    // Fallback to heuristic-based categorization
                    let questionLower = question.ToLower()
                    
                    let categoryList = [
                        ("Technical", ["code"; "develop"; "debug"; "architecture"; "algorithm"; "database"; "api"])
                        ("Behavioral", ["time"; "team"; "conflict"; "feedback"; "leadership"; "communication"])
                        ("Situational", ["would you"; "what if"; "handle"; "situation"; "scenario"; "release"])
                    ]
                    
                    categoryList
                    |> List.map (fun (category, keywords) ->
                        let matches = keywords |> List.filter (fun kw -> questionLower.Contains(kw)) |> List.length
                        let confidence = if matches = 0 then 0f else float32 matches / float32 keywords.Length * 100f |> min 100f
                        
                        {
                            Category = category
                            Confidence = confidence
                        })
                    |> List.filter (fun m -> m.Confidence > 20f)
                    |> List.sortByDescending (fun m -> m.Confidence)
            
            return categories
        }
