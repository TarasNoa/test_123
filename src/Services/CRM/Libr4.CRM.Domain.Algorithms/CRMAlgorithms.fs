namespace Libr4.CRM.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Infrastructure.AI

// Lead Scoring Algorithms
module LeadScorer =

    type LeadScore = {
        CustomerId: Guid
        Score: float
        Factors: Map<string, float>
    }

    type LeadFactors = {
        CompanySize: int
        Industry: string
        Budget: decimal
        Timeline: int  // days
        EngagementLevel: int
    }

    // Calculate lead score based on multiple factors
    let calculateLeadScore (factors: LeadFactors) : float =
        let companySizeScore = 
            match factors.CompanySize with
            | n when n < 10 -> 0.3
            | n when n < 50 -> 0.5
            | n when n < 200 -> 0.8
            | _ -> 1.0
        
        let industryScore = 
            match factors.Industry.ToLower() with
            | "technology" | "software" -> 1.0
            | "finance" | "healthcare" -> 0.8
            | "retail" | "manufacturing" -> 0.6
            | _ -> 0.4
        
        let budgetScore = 
            if factors.Budget < 10000m then 0.2
            elif factors.Budget < 50000m then 0.5
            elif factors.Budget < 100000m then 0.8
            else 1.0
        
        let timelineScore = 
            if factors.Timeline < 30 then 1.0
            elif factors.Timeline < 90 then 0.8
            elif factors.Timeline < 180 then 0.5
            else 0.3
        
        let engagementScore = float factors.EngagementLevel / 10.0
        
        // Weighted combination
        companySizeScore * 0.2 + industryScore * 0.25 + budgetScore * 0.25 + timelineScore * 0.15 + engagementScore * 0.15

    // Score multiple leads
    let scoreLeads (leads: (Guid * LeadFactors) list) : LeadScore list =
        leads
        |> List.map (fun (id, factors) ->
            let score = calculateLeadScore factors
            let factorsMap = 
                [
                    ("CompanySize", float factors.CompanySize)
                    ("Industry", 0.0) // Would need industry mapping
                    ("Budget", float factors.Budget)
                    ("Timeline", float factors.Timeline)
                    ("Engagement", float factors.EngagementLevel)
                ] |> Map.ofList
            
            {
                CustomerId = id
                Score = score
                Factors = factorsMap
            })
        |> List.sortByDescending (fun s -> s.Score)

    // Score leads using AI
    let scoreLeadsWithAI (aiService: IAIService) (leads: (Guid * LeadFactors) list) (marketContext: string) : Async<LeadScore list> =
        async {
            let leadsText = leads |> List.map (fun (id, f) -> sprintf "ID %s: size %d, industry %s, budget %.0f, timeline %d, engagement %d" (id.ToString()) f.CompanySize f.Industry f.Budget f.Timeline f.EngagementLevel) |> String.concat "; "
            
            let prompt = sprintf "Score leads for CRM: [%s], market context '%s'. Return JSON: {\"scores\": [{\"customerId\": string (guid), \"score\": number (0-1), \"factors\": {\"companySize\": number, \"industryScore\": number, \"budgetScore\": number, \"timelineScore\": number, \"engagementScore\": number}}]}" leadsText marketContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "crm") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let scores = 
                try
                    root.GetProperty("scores").EnumerateArray()
                    |> Seq.map (fun s ->
                        let customerId = Guid.Parse(s.GetProperty("customerId").GetString())
                        let score = s.GetProperty("score").GetDouble()
                        let factorsJson = s.GetProperty("factors")
                        let factors = 
                            [
                                ("CompanySize", factorsJson.GetProperty("companySize").GetDouble())
                                ("IndustryScore", factorsJson.GetProperty("industryScore").GetDouble())
                                ("BudgetScore", factorsJson.GetProperty("budgetScore").GetDouble())
                                ("TimelineScore", factorsJson.GetProperty("timelineScore").GetDouble())
                                ("EngagementScore", factorsJson.GetProperty("engagementScore").GetDouble())
                            ] |> Map.ofList
                        
                        {
                            CustomerId = customerId
                            Score = score
                            Factors = factors
                        })
                    |> List.ofSeq
                with _ ->
                    scoreLeads leads
            
            return scores |> List.sortByDescending (fun s -> s.Score)
        }

// Deal Forecasting
module DealForecaster =

    type DealForecast = {
        DealId: Guid
        ExpectedValue: decimal
        Probability: float
        ExpectedCloseDate: DateTime
    }

    type DealMetrics = {
        Stage: string
        Value: decimal
        DaysInStage: int
        HistoricalWinRate: float
    }

    // Forecast deal probability and expected value
    let forecastDeal (metrics: DealMetrics) : DealForecast =
        let stageProbability = 
            match metrics.Stage with
            | "Qualification" -> 0.2
            | "Proposal" -> 0.5
            | "Negotiation" -> 0.7
            | "ClosedWon" -> 1.0
            | "ClosedLost" -> 0.0
            | _ -> 0.1
        
        let timeDecay = 
            if metrics.DaysInStage > 90 then 0.8
            elif metrics.DaysInStage > 60 then 0.9
            else 1.0
        
        let probability = stageProbability * timeDecay * metrics.HistoricalWinRate
        let expectedValue = metrics.Value * decimal probability
        
        {
            DealId = Guid.Empty // Would be passed in real implementation
            ExpectedValue = expectedValue
            Probability = probability
            ExpectedCloseDate = DateTime.UtcNow.AddDays(float metrics.DaysInStage)
        }

    // Forecast total pipeline value
    let forecastPipeline (deals: DealMetrics list) : decimal =
        deals
        |> List.map forecastDeal
        |> List.sumBy (fun f -> f.ExpectedValue)

    // Forecast deals using AI
    let forecastDealsWithAI (aiService: IAIService) (deals: DealMetrics list) (historicalData: string) : Async<DealForecast list> =
        async {
            let dealsText = deals |> List.map (fun d -> sprintf "Stage %s, value %.0f, days %d, win rate %.2f" d.Stage d.Value d.DaysInStage d.HistoricalWinRate) |> String.concat "; "
            
            let prompt = sprintf "Forecast deals: [%s], historical data '%s'. Return JSON: {\"forecasts\": [{\"dealId\": string (guid), \"expectedValue\": number, \"probability\": number (0-1), \"expectedCloseDays\": number}]}" dealsText historicalData
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "crm") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let forecasts = 
                try
                    root.GetProperty("forecasts").EnumerateArray()
                    |> Seq.map (fun f ->
                        {
                            DealId = Guid.Parse(f.GetProperty("dealId").GetString())
                            ExpectedValue = decimal (f.GetProperty("expectedValue").GetDouble())
                            Probability = f.GetProperty("probability").GetDouble()
                            ExpectedCloseDate = DateTime.UtcNow.AddDays(f.GetProperty("expectedCloseDays").GetDouble())
                        })
                    |> List.ofSeq
                with _ ->
                    deals |> List.map forecastDeal
            
            return forecasts
        }

// Customer Segmentation
module CustomerSegmenter =

    type CustomerSegment = {
        Name: string
        Criteria: Map<string, string>
    }

    type CustomerProfile = {
        CustomerId: Guid
        LifetimeValue: decimal
        PurchaseFrequency: int
        LastPurchaseDate: DateTime
        ProductCategories: string list
    }

    // Segment customers based on RFM (Recency, Frequency, Monetary)
    let segmentCustomers (customers: CustomerProfile list) : Map<string, CustomerProfile list> =
        let now = DateTime.UtcNow
        
        customers
        |> List.groupBy (fun c ->
            let recencyDays = (now - c.LastPurchaseDate).Days
            let frequencyScore = 
                if c.PurchaseFrequency > 10 then "High"
                elif c.PurchaseFrequency > 5 then "Medium"
                else "Low"
            
            let monetaryScore = 
                if c.LifetimeValue > 100000m then "High"
                elif c.LifetimeValue > 10000m then "Medium"
                else "Low"
            
            let recencyScore = 
                if recencyDays < 30 then "Recent"
                elif recencyDays < 90 then "Moderate"
                else "Dormant"
            
            sprintf "%s_%s_%s" recencyScore frequencyScore monetaryScore)
        |> Map.ofList

    // Identify high-value customers
    let identifyHighValueCustomers (customers: CustomerProfile list) (threshold: decimal) : CustomerProfile list =
        customers
        |> List.filter (fun c -> c.LifetimeValue >= threshold)
        |> List.sortByDescending (fun c -> c.LifetimeValue)

    // Segment customers using AI
    let segmentCustomersWithAI (aiService: IAIService) (customers: CustomerProfile list) (businessGoals: string list) : Async<Map<string, CustomerProfile list>> =
        async {
            let customersText = customers |> List.map (fun c -> sprintf "ID %s, LTV %.0f, freq %d, categories [%s]" (c.CustomerId.ToString()) c.LifetimeValue c.PurchaseFrequency (String.concat ", " c.ProductCategories)) |> String.concat "; "
            let goalsText = String.concat ", " businessGoals
            
            let prompt = sprintf "Segment customers for goals [%s]: [%s]. Return JSON: {\"segments\": [{\"segmentName\": string, \"customerIds\": [string (guid)]}]}" goalsText customersText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "crm") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let segments = 
                try
                    root.GetProperty("segments").EnumerateArray()
                    |> Seq.map (fun s ->
                        let segmentName = s.GetProperty("segmentName").GetString()
                        let customerIds = 
                            s.GetProperty("customerIds").EnumerateArray()
                            |> Seq.map (fun id -> Guid.Parse(id.GetString()))
                            |> List.ofSeq
                        
                        let segmentCustomers = customers |> List.filter (fun c -> List.contains c.CustomerId customerIds)
                        (segmentName, segmentCustomers))
                    |> Map.ofSeq
                with _ ->
                    segmentCustomers customers
            
            return segments
        }

// Churn Prediction
module ChurnPredictor =

    type ChurnRisk = {
        CustomerId: Guid
        RiskLevel: string
        RiskScore: float
        RiskFactors: string list
    }

    type ActivityMetrics = {
        LoginFrequency: int
        FeatureUsage: int
        SupportTickets: int
        SatisfactionScore: float
    }

    // Predict churn risk based on activity metrics
    let predictChurnRisk (customerId: Guid) (metrics: ActivityMetrics) : ChurnRisk =
        let loginScore = 
            if metrics.LoginFrequency < 5 then 0.8
            elif metrics.LoginFrequency < 10 then 0.5
            else 0.1
        
        let usageScore = 
            if metrics.FeatureUsage < 3 then 0.7
            elif metrics.FeatureUsage < 5 then 0.4
            else 0.1
        
        let supportScore = 
            if metrics.SupportTickets > 5 then 0.6
            elif metrics.SupportTickets > 2 then 0.3
            else 0.1
        
        let satisfactionScore = 
            if metrics.SatisfactionScore < 3.0 then 0.8
            elif metrics.SatisfactionScore < 4.0 then 0.5
            else 0.1
        
        let riskScore = loginScore * 0.3 + usageScore * 0.3 + supportScore * 0.2 + satisfactionScore * 0.2
        
        let riskLevel = 
            if riskScore > 0.7 then "High"
            elif riskScore > 0.4 then "Medium"
            else "Low"
        
        let riskFactors = ResizeArray<string>()
        if loginScore > 0.5 then riskFactors.Add("Low login frequency")
        if usageScore > 0.5 then riskFactors.Add("Low feature usage")
        if supportScore > 0.5 then riskFactors.Add("High support tickets")
        if satisfactionScore > 0.5 then riskFactors.Add("Low satisfaction score")
        
        {
            CustomerId = customerId
            RiskLevel = riskLevel
            RiskScore = riskScore
            RiskFactors = List.ofSeq riskFactors
        }

    // Identify customers at risk of churn
    let identifyAtRiskCustomers (customers: (Guid * ActivityMetrics) list) : ChurnRisk list =
        customers
        |> List.map (fun (id, metrics) -> predictChurnRisk id metrics)
        |> List.filter (fun r -> r.RiskLevel = "High" || r.RiskLevel = "Medium")
        |> List.sortByDescending (fun r -> r.RiskScore)

    // Predict churn risk using AI
    let predictChurnRiskWithAI (aiService: IAIService) (customerId: Guid) (metrics: ActivityMetrics) (historicalBehavior: string list) : Async<ChurnRisk> =
        async {
            let behaviorText = String.concat ", " historicalBehavior
            let prompt = sprintf "Predict churn risk for customer %s: login %d, usage %d, tickets %d, satisfaction %.1f, behavior [%s]. Return JSON: {\"riskLevel\": \"Low/Medium/High\", \"riskScore\": number (0-1), \"riskFactors\": [string]}" (customerId.ToString()) metrics.LoginFrequency metrics.FeatureUsage metrics.SupportTickets metrics.SatisfactionScore behaviorText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "crm") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let riskLevel = 
                try root.GetProperty("riskLevel").GetString()
                with _ -> "Medium"
            
            let riskScore = 
                try root.GetProperty("riskScore").GetDouble()
                with _ -> 0.5
            
            let riskFactors = 
                try
                    root.GetProperty("riskFactors").EnumerateArray()
                    |> Seq.map (fun f -> f.GetString())
                    |> List.ofSeq
                with _ ->
                    predictChurnRisk customerId metrics |> fun r -> r.RiskFactors
            
            return {
                CustomerId = customerId
                RiskLevel = riskLevel
                RiskScore = riskScore
                RiskFactors = riskFactors
            }
        }
