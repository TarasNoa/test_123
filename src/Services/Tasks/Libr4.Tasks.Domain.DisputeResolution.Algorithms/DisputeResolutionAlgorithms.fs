namespace Libr4.Tasks.Domain.DisputeResolution.Algorithms

open System
open System.Text.Json
open Libr4.Tasks.Domain.DisputeResolution
open Libr4.AI.Application.Abstractions

// Dispute Classifier
module DisputeClassifier =

    type DisputeCategory = {
        Category: string
        Severity: string // Low, Medium, High
        EstimatedResolutionTime: string
        RecommendedAction: string
    }

    // Classify dispute and recommend handling strategy using AI
    let classifyDispute (aiService: IAIService) (disputeType: string) (description: string) : Async<DisputeCategory> =
        async {
            let prompt = sprintf "Classify dispute type '%s' with description: '%s'. Return JSON: {\"severity\": \"Low/Medium/High\", \"recommendedAction\": string}" disputeType description
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "dispute") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let severity = 
                try root.GetProperty("severity").GetString()
                with _ ->
                    match disputeType with
                    | "Payment" -> "High"
                    | "Quality" -> "Medium"
                    | "Scope" -> "Medium"
                    | "Communication" -> "Low"
                    | _ -> "Medium"
            
            let estimatedTime = 
                match severity with
                | "High" -> "5-10 business days"
                | "Medium" -> "3-7 business days"
                | "Low" -> "1-3 business days"
                | _ -> "3-5 business days"
            
            let recommendedAction = 
                try root.GetProperty("recommendedAction").GetString()
                with _ ->
                    match disputeType with
                    | "Payment" -> "Escalate to payment team for immediate review"
                    | "Quality" -> "Request evidence and arrange review meeting"
                    | "Scope" -> "Review original agreement and proposed changes"
                    | "Communication" -> "Facilitate direct communication between parties"
                    | _ -> "Standard dispute resolution process"
            
            return {
                Category = disputeType
                Severity = severity
                EstimatedResolutionTime = estimatedTime
                RecommendedAction = recommendedAction
            }
        }

// Resolution Strategy
module ResolutionStrategy =

    type ResolutionPlan = {
        Steps: string list
        RequiredParticipants: string list
        Timeline: string
        SuccessCriteria: string list
    }

    // Generate resolution strategy based on dispute type using AI
    let generateStrategy (aiService: IAIService) (disputeType: string) (description: string) : Async<ResolutionPlan> =
        async {
            let prompt = sprintf "Generate resolution strategy for dispute type '%s' with description: '%s'. Return JSON: {\"steps\": [string, string, string, string], \"requiredParticipants\": [string, string, string]}" disputeType description
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "dispute") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let steps = 
                try
                    root.GetProperty("steps").EnumerateArray()
                    |> Seq.map (fun s -> s.GetString())
                    |> List.ofSeq
                with _ ->
                    match disputeType with
                    | "Payment" -> 
                        ["Review payment terms"; "Verify work completion"; "Calculate correct amount"; "Process payment adjustment"]
                    | "Quality" -> 
                        ["Collect evidence from both parties"; "Review against specifications"; "Determine if rework needed"; "Agree on resolution"]
                    | "Scope" -> 
                        ["Review original scope"; "Document additional work"; "Negotiate compensation"; "Update agreement"]
                    | "Communication" -> 
                        ["Identify communication breakdown"; "Establish communication protocol"; "Schedule regular check-ins"; "Monitor progress"]
                    | _ -> 
                        ["Gather information"; "Assess situation"; "Propose solution"; "Implement resolution"]
            
            let requiredParticipants = 
                try
                    root.GetProperty("requiredParticipants").EnumerateArray()
                    |> Seq.map (fun p -> p.GetString())
                    |> List.ofSeq
                with _ ->
                    match disputeType with
                    | "Payment" -> ["Client"; "Freelancer"; "Payment Team"]
                    | "Quality" -> ["Client"; "Freelancer"; "Quality Reviewer"]
                    | "Scope" -> ["Client"; "Freelancer"; "Project Manager"]
                    | "Communication" -> ["Client"; "Freelancer"; "Mediator"]
                    | _ -> ["Client"; "Freelancer"; "Support Team"]
            
            let timeline = 
                match disputeType with
                | "Payment" -> "5-10 business days"
                | "Quality" -> "3-7 business days"
                | "Scope" -> "5-7 business days"
                | "Communication" -> "1-3 business days"
                | _ -> "3-5 business days"
            
            let successCriteria = 
                match disputeType with
                | "Payment" -> ["Correct payment processed"; "Both parties satisfied"; "No further disputes"]
                | "Quality" -> ["Quality standards met"; "Work accepted"; "Payment released"]
                | "Scope" -> ["Scope clarified"; "Compensation agreed"; "Agreement updated"]
                | "Communication" -> ["Communication improved"; "Both parties satisfied"; "Project on track"]
                | _ -> ["Issue resolved"; "Both parties agree"; "Project continues"]
            
            return {
                Steps = steps
                RequiredParticipants = requiredParticipants
                Timeline = timeline
                SuccessCriteria = successCriteria
            }
        }

// Evidence Analyzer
module EvidenceAnalyzer =

    type EvidenceAssessment = {
        RelevantEvidence: bool
        Strength: string // Weak, Moderate, Strong
        Gaps: string list
        Recommendation: string
    }

    // Analyze evidence submitted for dispute using AI
    let analyzeEvidence (aiService: IAIService) (evidence: (string * string * string) list) (disputeType: string) : Async<EvidenceAssessment> =
        async {
            let relevantEvidence = evidence |> List.isEmpty |> not
            
            let strength = 
                if evidence.Length >= 3 then "Strong"
                elif evidence.Length >= 2 then "Moderate"
                elif evidence.Length >= 1 then "Weak"
                else "Insufficient"
            
            let requiredEvidenceTypes = 
                match disputeType with
                | "Payment" -> ["Payment records"; "Work completion proof"]
                | "Quality" -> ["Screenshots"; "Specifications"; "Deliverables"]
                | "Scope" -> ["Original agreement"; "Change requests"; "Communication logs"]
                | "Communication" -> ["Chat logs"; "Email records"]
                | _ -> ["Any supporting documentation"]
            
            let gaps = 
                requiredEvidenceTypes
                |> List.filter (fun req -> not (evidence |> List.exists (fun (et, _, _) -> et = req)))
            
            let evidenceText = evidence |> List.map (fun (et, _, _) -> et) |> String.concat ", "
            let gapsText = gaps |> String.concat ", "
            let prompt = sprintf "Analyze evidence for dispute type '%s': evidence [%s], gaps [%s]. Return JSON: {\"recommendation\": string}" disputeType evidenceText gapsText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "evidence") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let recommendation = 
                try root.GetProperty("recommendation").GetString()
                with _ ->
                    if gaps.IsEmpty then "Evidence is sufficient for resolution"
                    elif gaps.Length = 1 then sprintf "Need additional evidence: %s" gaps.[0]
                    else sprintf "Need additional evidence: %s" (String.concat ", " gaps)
            
            return {
                RelevantEvidence = relevantEvidence
                Strength = strength
                Gaps = gaps
                Recommendation = recommendation
            }
        }
