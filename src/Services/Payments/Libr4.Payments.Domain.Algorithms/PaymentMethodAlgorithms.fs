namespace Libr4.Payments.Domain.Algorithms

open System
open System.Text.Json
open Libr4.Payments.Domain.PaymentMethods
open Libr4.AI.Infrastructure.AI

// Payment Method Validator
module PaymentMethodValidator =

    type ValidationResult = {
        IsValid: bool
        Errors: string list
    }

    // Validate card expiration
    let validateCardExpiration (expMonth: int) (expYear: int) : ValidationResult =
        let now = DateTime.UtcNow
        let expiration = DateTime(expYear, expMonth, 1).AddMonths(1).AddDays(-1)
        
        if expiration < now then
            {
                IsValid = false
                Errors = ["Card has expired"]
            }
        elif (expiration - now).TotalDays > 365.0 * 20.0 then
            {
                IsValid = false
                Errors = ["Expiration date too far in the future"]
            }
        else
            {
                IsValid = true
                Errors = []
            }

    // Validate card number format (Luhn algorithm placeholder)
    let validateCardNumber (last4: string) : ValidationResult =
        if String.IsNullOrEmpty(last4) || last4.Length <> 4 then
            {
                IsValid = false
                Errors = ["Invalid last 4 digits format"]
            }
        elif not (System.Text.RegularExpressions.Regex.IsMatch(last4, @"^\d{4}$")) then
            {
                IsValid = false
                Errors = ["Last 4 digits must be numeric"]
            }
        else
            {
                IsValid = true
                Errors = []
            }

// Security Analyzer
module SecurityAnalyzer =

    type SecurityRisk = {
        Level: string
        Score: float
        Factors: string list
    }

    // Analyze payment method security
    let analyzeSecurity (createdAt: DateTime) (lastUsed: DateTime option) (isDefault: bool) : SecurityRisk =
        let now = DateTime.UtcNow
        let factors = ResizeArray<string>()
        let mutable score = 100.0
        
        // Check if method is old and unused
        match lastUsed with
        | Some last when (now - last).TotalDays > 365.0 ->
            score <- score - 20.0
            factors.Add("Payment method not used in over a year")
        | None when (now - createdAt).TotalDays > 30.0 ->
            score <- score - 15.0
            factors.Add("Payment method never used and is over 30 days old")
        | _ -> ()
        
        // Check if method is default (higher risk if compromised)
        if isDefault then
            score <- score - 5.0
        
        let riskLevel = 
            match score with
            | _ when score >= 80.0 -> "Low"
            | _ when score >= 50.0 -> "Medium"
            | _ -> "High"
        
        {
            Level = riskLevel
            Score = score
            Factors = List.ofSeq factors
        }

    // Analyze security using AI for intelligent risk assessment
    let analyzeSecurityWithAI (aiService: IAIService) (createdAt: DateTime) (lastUsed: DateTime option) (isDefault: bool) (transactionHistory: string) : Async<SecurityRisk> =
        async {
            let now = DateTime.UtcNow
            let lastUsedText = match lastUsed with | Some d -> d.ToString("o") | None -> "Never"
            
            let prompt = sprintf "Analyze payment method security: created %s, last used %s, is default %b, history '%s'. Return JSON: {\"level\": \"Low/Medium/High\", \"score\": number (0-100), \"factors\": [string]}" (createdAt.ToString("o")) lastUsedText isDefault transactionHistory
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "payments") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let level = try root.GetProperty("level").GetString() with _ -> "Medium"
            let score = try root.GetProperty("score").GetDouble() with _ -> 50.0
            let factors = 
                try
                    root.GetProperty("factors").EnumerateArray()
                    |> Seq.map (fun f -> f.GetString())
                    |> List.ofSeq
                with _ ->
                    let fallbackFactors = ResizeArray<string>()
                    match lastUsed with
                    | Some last when (now - last).TotalDays > 365.0 -> fallbackFactors.Add("Payment method not used in over a year")
                    | None when (now - createdAt).TotalDays > 30.0 -> fallbackFactors.Add("Payment method never used and is over 30 days old")
                    | _ -> ()
                    if isDefault then fallbackFactors.Add("Payment method is default")
                    List.ofSeq fallbackFactors
            
            return {
                Level = level
                Score = score
                Factors = factors
            }
        }

// PCI DSS Compliance Checker
module PciDssComplianceChecker =

    type ComplianceStatus = {
        IsCompliant: bool
        Violations: string list
        Recommendations: string list
    }

    // Check PCI DSS compliance
    let checkCompliance (paymentMethodType: PaymentMethodType) (hasTokenization: bool) (hasEncryption: bool) : ComplianceStatus =
        let violations = ResizeArray<string>()
        let recommendations = ResizeArray<string>()
        
        // Check tokenization
        if paymentMethodType = PaymentMethodType.Card && not hasTokenization then
            violations.Add("Card data not tokenized (PCI DSS requirement)")
            recommendations.Add("Implement tokenization for card data")
        
        // Check encryption
        if paymentMethodType = PaymentMethodType.Card && not hasEncryption then
            violations.Add("Card data not encrypted at rest (PCI DSS requirement)")
            recommendations.Add("Implement AES-256 encryption for card data")
        
        // General recommendations
        if paymentMethodType = PaymentMethodType.Card then
            recommendations.Add("Implement fraud detection")
            recommendations.Add("Regular PCI DSS audits required")
        
        {
            IsCompliant = violations.Count = 0
            Violations = List.ofSeq violations
            Recommendations = List.ofSeq recommendations
        }

    // Check compliance using AI for intelligent assessment
    let checkComplianceWithAI (aiService: IAIService) (paymentMethodType: PaymentMethodType) (hasTokenization: bool) (hasEncryption: bool) (securityContext: string) : Async<ComplianceStatus> =
        async {
            let prompt = sprintf "Check PCI DSS compliance: type '%s', has tokenization %b, has encryption %b, context '%s'. Return JSON: {\"isCompliant\": bool, \"violations\": [string], \"recommendations\": [string]}" (string paymentMethodType) hasTokenization hasEncryption securityContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "payments") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let isCompliant = try root.GetProperty("isCompliant").GetBoolean() with _ -> true
            let violations = 
                try
                    root.GetProperty("violations").EnumerateArray()
                    |> Seq.map (fun v -> v.GetString())
                    |> List.ofSeq
                with _ ->
                    let fallbackViolations = ResizeArray<string>()
                    if paymentMethodType = PaymentMethodType.Card && not hasTokenization then
                        fallbackViolations.Add("Card data not tokenized (PCI DSS requirement)")
                    if paymentMethodType = PaymentMethodType.Card && not hasEncryption then
                        fallbackViolations.Add("Card data not encrypted at rest (PCI DSS requirement)")
                    List.ofSeq fallbackViolations
            
            let recommendations = 
                try
                    root.GetProperty("recommendations").EnumerateArray()
                    |> Seq.map (fun r -> r.GetString())
                    |> List.ofSeq
                with _ ->
                    let fallbackRecommendations = ResizeArray<string>()
                    if paymentMethodType = PaymentMethodType.Card then
                        fallbackRecommendations.Add("Implement fraud detection")
                        fallbackRecommendations.Add("Regular PCI DSS audits required")
                    if not hasTokenization then fallbackRecommendations.Add("Implement tokenization for card data")
                    if not hasEncryption then fallbackRecommendations.Add("Implement AES-256 encryption for card data")
                    List.ofSeq fallbackRecommendations
            
            return {
                IsCompliant = isCompliant
                Violations = violations
                Recommendations = recommendations
            }
        }

// Payment Method Recommender
module PaymentMethodRecommender =

    type Recommendation = {
        MethodType: PaymentMethodType
        Confidence: float
        Reason: string
    }

    // Recommend payment method based on transaction characteristics
    let recommendMethod (transactionAmount: float) (isRecurring: bool) (userCountry: string) : Recommendation list =
        let recommendations = ResizeArray<Recommendation>()
        
        // Card is generally good for most transactions
        if transactionAmount < 10000.0 then
            recommendations.Add({
                MethodType = PaymentMethodType.Card
                Confidence = 0.85
                Reason = "Card is widely accepted and convenient"
            })
        
        // Bank transfer for large amounts
        if transactionAmount >= 10000.0 then
            recommendations.Add({
                MethodType = PaymentMethodType.BankTransfer
                Confidence = 0.90
                Reason = "Bank transfer is safer for large transactions"
            })
        
        // Wallet for recurring payments
        if isRecurring then
            recommendations.Add({
                MethodType = PaymentMethodType.Wallet
                Confidence = 0.75
                Reason = "Wallet is convenient for recurring payments"
            })
        
        // If no recommendations yet, suggest card as default
        if recommendations.Count = 0 then
            recommendations.Add({
                MethodType = PaymentMethodType.Card
                Confidence = 0.60
                Reason = "Card is the default recommended method"
            })
        
        List.ofSeq recommendations

    // Recommend payment method using AI for intelligent selection
    let recommendMethodWithAI (aiService: IAIService) (transactionAmount: float) (isRecurring: bool) (userCountry: string) (transactionContext: string) : Async<Recommendation list> =
        async {
            let prompt = sprintf "Recommend payment method: amount %.2f, recurring %b, country '%s', context '%s'. Return JSON: {\"recommendations\": [{\"methodType\": string (Card/BankTransfer/Wallet), \"confidence\": number (0-1), \"reason\": string}]}" transactionAmount isRecurring userCountry transactionContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "payments") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let recommendations = 
                try
                    root.GetProperty("recommendations").EnumerateArray()
                    |> Seq.map (fun r ->
                        let methodTypeStr = r.GetProperty("methodType").GetString()
                        let methodType = 
                            match methodTypeStr with
                            | "BankTransfer" -> PaymentMethodType.BankTransfer
                            | "Wallet" -> PaymentMethodType.Wallet
                            | _ -> PaymentMethodType.Card
                        let confidence = r.GetProperty("confidence").GetDouble()
                        let reason = r.GetProperty("reason").GetString()
                        
                        {
                            MethodType = methodType
                            Confidence = confidence
                            Reason = reason
                        })
                    |> List.ofSeq
                with _ ->
                    recommendMethod transactionAmount isRecurring userCountry
            
            return recommendations |> List.sortByDescending (fun r -> r.Confidence)
        }
