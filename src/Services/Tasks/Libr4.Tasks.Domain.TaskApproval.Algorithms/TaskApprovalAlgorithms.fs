namespace Libr4.Tasks.Domain.TaskApproval.Algorithms

open System
open System.Text.Json
open Libr4.Tasks.Domain.TaskApproval
open Libr4.AI.Infrastructure.AI

// Completion Verifier
module CompletionVerifier =

    type VerificationResult = {
        IsComplete: bool
        CompletionPercentage: float32
        OutstandingTasks: string list
        Recommendation: string
    }

    // Verify task completion before approval using AI
    let verifyCompletion (aiService: IAIService) (deliverables: string list) (milestones: (string * bool) list) : Async<VerificationResult> =
        async {
            let completedMilestones = milestones |> List.filter snd |> List.length
            let totalMilestones = milestones.Length
            let completionPercentage = 
                if totalMilestones > 0 then float32 completedMilestones / float32 totalMilestones * 100f
                else 0f
            
            let isComplete = completionPercentage >= 100f && deliverables.IsEmpty |> not
            
            let outstandingTasks = 
                milestones
                |> List.filter (fun (_, completed) -> not completed)
                |> List.map fst
            
            let milestonesText = milestones |> List.map (fun (m, c) -> sprintf "%s:%b" m c) |> String.concat ", "
            let prompt = sprintf "Verify task completion: %d%% complete, milestones [%s], %d deliverables. Return JSON: {\"recommendation\": string}" (int completionPercentage) milestonesText deliverables.Length
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "approval") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let recommendation = 
                try root.GetProperty("recommendation").GetString()
                with _ ->
                    if isComplete then "Task is complete and ready for approval"
                    elif completionPercentage >= 80f then "Task is nearly complete, review outstanding items"
                    elif completionPercentage >= 50f then "Task is partially complete, significant work remains"
                    else "Task is not ready for approval"
            
            return {
                IsComplete = isComplete
                CompletionPercentage = completionPercentage
                OutstandingTasks = outstandingTasks
                Recommendation = recommendation
            }
        }

// Payment Calculator
module PaymentCalculator =

    type PaymentCalculation = {
        BaseAmount: int
        BonusAmount: int
        PenaltyAmount: int
        FinalAmount: int
        Breakdown: string list
    }

    // Calculate final payment based on performance using AI
    let calculatePayment (aiService: IAIService) (agreedAmount: int) (completionPercentage: float32) (qualityScore: float32) (earlyCompletionDays: int) : Async<PaymentCalculation> =
        async {
            let baseAmount = agreedAmount
            
            let prompt = sprintf "Calculate payment: base $%d, completion %.1f%%, quality %.1f/5, %d days early/late. Return JSON: {\"bonusAmount\": number, \"penaltyAmount\": number}" agreedAmount completionPercentage qualityScore earlyCompletionDays
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "payment") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let bonusAmount = 
                try root.GetProperty("bonusAmount").GetInt32()
                with _ ->
                    let completionBonus = if completionPercentage >= 100f then int (float32 agreedAmount * 0.1f) else 0
                    let qualityBonus = if qualityScore >= 4.5f then int (float32 agreedAmount * 0.05f) else 0
                    let earlyBonus = if earlyCompletionDays > 0 then earlyCompletionDays * 10 else 0
                    completionBonus + qualityBonus + earlyBonus
            
            let penaltyAmount = 
                try root.GetProperty("penaltyAmount").GetInt32()
                with _ ->
                    let latePenalty = if earlyCompletionDays < 0 then abs earlyCompletionDays * 20 else 0
                    let qualityPenalty = if qualityScore < 3.0f then int (float32 agreedAmount * 0.1f) else 0
                    latePenalty + qualityPenalty
            
            let finalAmount = baseAmount + bonusAmount - penaltyAmount |> max 0
            
            let breakdown = 
                [sprintf "Base amount: $%d" baseAmount]
                @ (if bonusAmount > 0 then [sprintf "Bonus: +$%d" bonusAmount] else [])
                @ (if penaltyAmount > 0 then [sprintf "Penalty: -$%d" penaltyAmount] else [])
                @ [sprintf "Final amount: $%d" finalAmount]
            
            return {
                BaseAmount = baseAmount
                BonusAmount = bonusAmount
                PenaltyAmount = penaltyAmount
                FinalAmount = finalAmount
                Breakdown = breakdown
            }
        }

// Approval Workflow
module ApprovalWorkflow =

    type WorkflowStep = {
        Step: string
        Required: bool
        Completed: bool
        Notes: string
    }

    type WorkflowStatus = {
        CanApprove: bool
        Steps: WorkflowStep list
        BlockingIssues: string list
    }

    // Check approval workflow status
    let checkWorkflow (deliverablesSubmitted: bool) (clientReviewed: bool) (freelancerConfirmed: bool) (disputesResolved: bool) : WorkflowStatus =
        let steps = [
            {
                Step = "Deliverables Submitted"
                Required = true
                Completed = deliverablesSubmitted
                Notes = if deliverablesSubmitted then "All deliverables received" else "Waiting for deliverables"
            }
            {
                Step = "Client Review"
                Required = true
                Completed = clientReviewed
                Notes = if clientReviewed then "Client has reviewed work" else "Client review pending"
            }
            {
                Step = "Freelancer Confirmation"
                Required = true
                Completed = freelancerConfirmed
                Notes = if freelancerConfirmed then "Freelancer confirmed completion" else "Freelancer confirmation pending"
            }
            {
                Step = "Dispute Resolution"
                Required = false
                Completed = disputesResolved
                Notes = if disputesResolved then "No active disputes" else "Check for active disputes"
            }
        ]
        
        let requiredSteps = steps |> List.filter (fun s -> s.Required)
        let blockingIssues = requiredSteps |> List.filter (fun s -> not s.Completed) |> List.map (fun s -> sprintf "%s not completed" s.Step)
        
        let canApprove = blockingIssues.IsEmpty
        
        {
            CanApprove = canApprove
            Steps = steps
            BlockingIssues = blockingIssues
        }
