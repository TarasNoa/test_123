namespace Libr4.Payments.Domain.FSharp

open System

/// Domain types for financial calculations
type Money = {
    Amount: decimal
    Currency: string
}

type TaxRate = {
    Name: string
    Rate: decimal
    Region: string option
}

type DiscountType =
    | Percentage of decimal
    | FixedAmount of Money
    | Tiered of (decimal * DiscountType) list

type PaymentStatus =
    | Pending
    | Processing
    | Completed of completedAt: DateTime
    | Failed of reason: string
    | Refunded of refundAmount: Money

type EscrowState =
    | Created
    | Funded of amount: Money
    | Held
    | Released
    | Disputed

type InvoiceLineItem = {
    Description: string
    Quantity: decimal
    UnitPrice: Money
    TaxRates: TaxRate list
    Discount: DiscountType option
}

/// Pure functional financial calculation module
module FinancialCalculations =
    /// Calculate tax for a money amount
    let calculateTax (money: Money) (taxRate: TaxRate) : Money =
        let taxAmount = money.Amount * taxRate.Rate
        { Amount = taxAmount; Currency = money.Currency }
    
    /// Calculate total with multiple taxes
    let calculateTotalTax (money: Money) (taxRates: TaxRate list) : Money =
        taxRates
        |> List.fold (fun acc rate -> 
            let tax = calculateTax money rate
            { Amount = acc.Amount + tax.Amount; Currency = money.Currency }
        ) { Amount = 0m; Currency = money.Currency }
    
    /// Apply discount to money amount
    let applyDiscount (money: Money) (discount: DiscountType) : Money =
        match discount with
        | Percentage percent ->
            let discountAmount = money.Amount * percent
            { Amount = money.Amount - discountAmount; Currency = money.Currency }
        | FixedAmount fixedDiscount ->
            { Amount = max 0m (money.Amount - fixedDiscount.Amount); Currency = money.Currency }
        | Tiered tiers ->
            // Find applicable tier
            let applicableTier = 
                tiers
                |> List.tryFind (fun (threshold, _) -> money.Amount >= threshold)
            
            match applicableTier with
            | Some (_, tierDiscount) -> applyDiscount money tierDiscount
            | None -> money
    
    /// Calculate line item total (quantity * price - discount + tax)
    let calculateLineItemTotal (lineItem: InvoiceLineItem) : Money * Money =
        // Base amount
        let baseAmount = lineItem.UnitPrice.Amount * lineItem.Quantity
        let baseMoney = { Amount = baseAmount; Currency = lineItem.UnitPrice.Currency }
        
        // Apply discount
        let afterDiscount = 
            match lineItem.Discount with
            | Some discount -> applyDiscount baseMoney discount
            | None -> baseMoney
        
        // Calculate tax
        let totalTax = calculateTotalTax afterDiscount lineItem.TaxRates
        
        // Final total
        let finalTotal = { Amount = afterDiscount.Amount + totalTax.Amount; Currency = baseMoney.Currency }
        
        (finalTotal, totalTax)
    
    /// Calculate invoice totals
    let calculateInvoiceTotals (lineItems: InvoiceLineItem list) : Money * Money * Money =
        let results = lineItems |> List.map calculateLineItemTotal
        
        let subtotal = 
            results
            |> List.sumBy (fun (total, _) -> total.Amount)
            |> fun amount -> { Amount = amount; Currency = (lineItems |> List.head).UnitPrice.Currency }
        
        let totalTax =
            results
            |> List.sumBy (fun (_, tax) -> tax.Amount)
            |> fun amount -> { Amount = amount; Currency = subtotal.Currency }
        
        let grandTotal = { Amount = subtotal.Amount + totalTax.Amount; Currency = subtotal.Currency }
        
        (subtotal, totalTax, grandTotal)
    
    /// Validate payment amount (business rules)
    let validatePaymentAmount (amount: Money) (minAmount: decimal) (maxAmount: decimal) : Result<Money, string> =
        if amount.Amount <= 0m then
            Error "Payment amount must be positive"
        elif amount.Amount < minAmount then
            Error $"Payment amount below minimum of {minAmount}"
        elif amount.Amount > maxAmount then
            Error $"Payment amount exceeds maximum of {maxAmount}"
        else
            Ok amount
    
    /// Calculate escrow release schedule
    let calculateEscrowSchedule (totalAmount: Money) (milestones: (string * decimal) list) : (string * Money) list =
        milestones
        |> List.map (fun (name, percentage) ->
            let milestoneAmount = { Amount = totalAmount.Amount * percentage; Currency = totalAmount.Currency }
            (name, milestoneAmount)
        )
    
    /// Detect suspicious transaction patterns
    let detectAnomalies (transactions: Money list) : string list =
        if transactions |> List.isEmpty then
            []
        else
            let amounts = transactions |> List.map (fun m -> m.Amount)
            let avg = amounts |> List.average
            let stdDev = 
                let variance = amounts |> List.averageBy (fun x -> (x - avg) ** 2.0m)
                sqrt (float variance) |> decimal
            
            let anomalies = 
                transactions
                |> List.mapi (fun i m ->
                    let deviation = abs (m.Amount - avg) / stdDev
                    if deviation > 3.0m then // 3 sigma rule
                        Some $"Transaction {i} is {deviation:F2} std dev from mean"
                    else
                        None
                )
                |> List.choose id
            
            anomalies
    
    /// Convert currency (simplified - real implementation would use exchange rates)
    let convertCurrency (money: Money) (targetCurrency: string) (rate: decimal) : Money =
        { Amount = money.Amount * rate; Currency = targetCurrency }
    
    /// Calculate late payment penalty
    let calculateLateFee (originalAmount: Money) (daysLate: int) (dailyRate: decimal) : Money =
        let fee = originalAmount.Amount * dailyRate * (decimal daysLate)
        { Amount = fee; Currency = originalAmount.Currency }
    
    /// Prorate amount based on time period
    let prorateAmount (fullAmount: Money) (daysUsed: int) (totalDays: int) : Money =
        if totalDays <= 0 then
            fullAmount
        else
            let prorated = fullAmount.Amount * (decimal daysUsed / decimal totalDays)
            { Amount = prorated; Currency = fullAmount.Currency }
    
    /// Validate tax calculation (sanity check)
    let validateTaxCalculation (subtotal: Money) (tax: Money) (maxRate: decimal) : bool =
        let calculatedRate = tax.Amount / subtotal.Amount
        calculatedRate <= maxRate

/// Module for escrow-specific logic
module EscrowLogic =
    /// State machine for escrow transitions
    type EscrowTransition =
        | Fund of Money
        | Release
        | Dispute of reason: string
        | Resolve of release: bool
    
    /// Apply transition to escrow state
    let applyTransition (state: EscrowState) (transition: EscrowTransition) : Result<EscrowState, string> =
        match state, transition with
        | Created, Fund amount ->
            Ok (Funded amount)
        | Funded amount, Release ->
            if amount.Amount > 0m then
                Ok Held
            else
                Error "Cannot release unfunded escrow"
        | Held, Release ->
            Ok Released
        | Held, Dispute reason ->
            Ok Disputed
        | Disputed, Resolve shouldRelease ->
            if shouldRelease then Ok Released else Error "Escrow dispute not resolved"
        | _, _ ->
            Error $"Invalid transition from {state} with {transition}"
    
    /// Calculate dispute resolution amount
    let calculateDisputeResolution (totalAmount: Money) (percentages: (string * decimal) list) : (string * Money) list =
        percentages
        |> List.map (fun (party, percentage) ->
            let amount = { Amount = totalAmount.Amount * percentage; Currency = totalAmount.Currency }
            (party, amount)
        )

/// Fraud detection using statistical analysis
module FraudDetection =
    type RiskScore =
        | Low of score: decimal
        | Medium of score: decimal
        | High of score: decimal
        | Critical of score: decimal
    
    /// Calculate risk score for transaction
    let calculateRiskScore 
        (amount: Money) 
        (userHistory: Money list) 
        (velocity: int) // transactions per hour
        : RiskScore =
        
        let baseScore = 0m
        
        // Amount anomaly
        let amountScore =
            if userHistory |> List.isEmpty then
                20m // New user
            else
                let avg = userHistory |> List.averageBy (fun m -> m.Amount)
                let max = userHistory |> List.maxBy (fun m -> m.Amount)
                if amount.Amount > max.Amount * 2m then
                    50m // Double previous max
                elif amount.Amount > avg * 3m then
                    30m // 3x average
                else
                    0m
        
        // Velocity check
        let velocityScore =
            if velocity > 10 then 40m
            elif velocity > 5 then 20m
            else 0m
        
        let totalScore = baseScore + amountScore + velocityScore
        
        match totalScore with
        | s when s < 20m -> Low s
        | s when s < 40m -> Medium s
        | s when s < 60m -> High s
        | s -> Critical s
    
    /// Determine if transaction requires manual review
    let requiresManualReview (riskScore: RiskScore) : bool =
        match riskScore with
        | High _ | Critical _ -> true
        | _ -> false
