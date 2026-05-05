namespace Libr4.Analytics.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Infrastructure.AI

// Types (must be defined before use)
type TrendDirection = Upward | Downward | Neutral

// Alert Condition Types
type AlertCondition =
    | GreaterThan of string * float
    | LessThan of string * float
    | EqualTo of string * float
    | Between of string * float * float
    | PercentageChange of string * float
    | CustomCondition of (Map<string, obj> -> bool)

// Alert Evaluation Algorithms
module AlertEvaluator =

    // Evaluate a single alert condition against metric data
    let evaluateCondition (condition: AlertCondition) (metrics: Map<string, obj>) : bool =
        match condition with
        | GreaterThan (metricName, threshold) ->
            match metrics.TryFind metricName with
            | Some value ->
                match value with
                | :? float as floatValue -> floatValue > threshold
                | :? decimal as decimalValue -> float decimalValue > threshold
                | :? int as intValue -> float intValue > threshold
                | _ -> false
            | None -> false
        | LessThan (metricName, threshold) ->
            match metrics.TryFind metricName with
            | Some value ->
                match value with
                | :? float as floatValue -> floatValue < threshold
                | :? decimal as decimalValue -> float decimalValue < threshold
                | :? int as intValue -> float intValue < threshold
                | _ -> false
            | None -> false
        | EqualTo (metricName, threshold) ->
            match metrics.TryFind metricName with
            | Some value ->
                match value with
                | :? float as floatValue -> abs (floatValue - threshold) < 0.0001
                | :? decimal as decimalValue -> abs (float decimalValue - threshold) < 0.0001
                | :? int as intValue -> float intValue = threshold
                | _ -> false
            | None -> false
        | Between (metricName, minThreshold, maxThreshold) ->
            match metrics.TryFind metricName with
            | Some value ->
                match value with
                | :? float as floatValue -> floatValue >= minThreshold && floatValue <= maxThreshold
                | :? decimal as decimalValue -> float decimalValue >= minThreshold && float decimalValue <= maxThreshold
                | :? int as intValue -> float intValue >= minThreshold && float intValue <= maxThreshold
                | _ -> false
            | None -> false
        | PercentageChange (metricName, threshold) ->
            match metrics.TryFind metricName, metrics.TryFind (metricName + "_previous") with
            | Some currentValue, Some previousValue ->
                let currentFloat = 
                    match currentValue with
                    | :? float as f -> f
                    | :? decimal as d -> float d
                    | :? int as i -> float i
                    | _ -> 0.0
                let previousFloat =
                    match previousValue with
                    | :? float as f -> f
                    | :? decimal as d -> float d
                    | :? int as i -> float i
                    | _ -> 0.0
                if previousFloat = 0.0 then false
                else abs ((currentFloat - previousFloat) / previousFloat * 100.0) > threshold
            | _ -> false
        | CustomCondition evaluator ->
            evaluator metrics

    // Generate smart alert suggestions using AI
    let generateAlertSuggestions (aiService: IAIService) (metrics: Map<string, obj>) (metricName: string) : Async<string list> =
        async {
            let metricsText = metrics |> Map.map (fun k v -> sprintf "%s: %A" k v) |> Map.values |> String.concat ", "
            let prompt = sprintf "Suggest alert conditions for metric '%s' with current values: [%s]. Return JSON: {\"suggestions\": [string, string, string]}" metricName metricsText
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "analytics") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let suggestions = 
                try
                    root.GetProperty("suggestions").EnumerateArray()
                    |> Seq.map (fun s -> s.GetString())
                    |> List.ofSeq
                with _ ->
                    [
                        sprintf "Alert when %s exceeds average by 20%%" metricName
                        sprintf "Alert when %s drops below minimum threshold" metricName
                        sprintf "Alert when %s shows sudden spike" metricName
                    ]
            
            return suggestions
        }

    // Evaluate multiple alert conditions (AND logic)
    let evaluateAllConditions (conditions: AlertCondition list) (metrics: Map<string, obj>) : bool =
        conditions |> List.forall (fun cond -> evaluateCondition cond metrics)

    // Evaluate multiple alert conditions (OR logic)
    let evaluateAnyCondition (conditions: AlertCondition list) (metrics: Map<string, obj>) : bool =
        conditions |> List.exists (fun cond -> evaluateCondition cond metrics)

// Data Aggregation Algorithms
module DataAggregator =

    // Calculate average from a list of values
    let calculateAverage (values: float list) : float =
        match values with
        | [] -> 0.0
        | _ -> List.average values

    // Calculate median from a list of values
    let calculateMedian (values: float list) : float =
        match values with
        | [] -> 0.0
        | _ ->
            let sorted = values |> List.sort
            let count = List.length sorted
            if count % 2 = 0 then
                let mid1 = sorted.[count / 2 - 1]
                let mid2 = sorted.[count / 2]
                (mid1 + mid2) / 2.0
            else
                sorted.[count / 2]

    // Calculate standard deviation
    let calculateStandardDeviation (values: float list) : float =
        match values with
        | [] -> 0.0
        | _ ->
            let avg = calculateAverage values
            let variance = values |> List.averageBy (fun v -> (v - avg) ** 2.0)
            sqrt variance

    // Calculate percentile
    let calculatePercentile (values: float list) (percentile: float) : float =
        match values with
        | [] -> 0.0
        | _ ->
            let sorted = values |> List.sort
            let count = List.length sorted
            let index = int (float (count - 1) * percentile / 100.0)
            sorted.[index]

    // Calculate moving average
    let calculateMovingAverage (values: float list) (windowSize: int) : float list =
        match values with
        | [] -> []
        | _ ->
            seq { windowSize .. List.length values }
            |> Seq.map (fun i ->
                let window = values.[i - windowSize .. i - 1]
                calculateAverage window)
            |> List.ofSeq

// Trend Analysis Algorithms
module TrendAnalyzer =

    // Determine trend direction from a series of values using AI
    let determineTrend (aiService: IAIService) (values: float list) : Async<TrendDirection> =
        async {
            if values.IsEmpty || values.Length = 1 then
                return Neutral
            else
                let valuesText = values |> List.map string |> String.concat ", "
                let prompt = sprintf "Determine trend direction for values: [%s]. Return JSON: {\"trend\": \"Upward/Downward/Neutral\"}" valuesText
                
                let! aiResponse = aiService.AnalyzeTextAsync(prompt, "analytics") |> Async.AwaitTask
                
                let jsonDoc = JsonDocument.Parse(aiResponse)
                let root = jsonDoc.RootElement
                
                let trend = 
                    try root.GetProperty("trend").GetString()
                    with _ ->
                        let first = values |> List.head
                        let last = values |> List.last
                        let change = (last - first) / first * 100.0
                        match change with
                        | c when c > 5.0 -> "Upward"
                        | c when c < -5.0 -> "Downward"
                        | _ -> "Neutral"
                
                return 
                    match trend with
                    | "Upward" -> Upward
                    | "Downward" -> Downward
                    | _ -> Neutral
        }

    // Calculate trend strength (0.0 to 1.0)
    let calculateTrendStrength (values: float list) : float =
        match values with
        | [] -> 0.0
        | _ ->
            let stdDev = DataAggregator.calculateStandardDeviation values
            let avg = DataAggregator.calculateAverage values
            if avg = 0.0 then 0.0
            else min 1.0 (stdDev / abs avg)

    // Detect anomalies using AI-enhanced analysis
    let detectAnomalies (aiService: IAIService) (values: float list) (threshold: float) : Async<(int * float) list> =
        async {
            if values.IsEmpty then
                return []
            else
                let valuesText = values |> List.map string |> String.concat ", "
                let prompt = sprintf "Detect anomalies in values: [%s]. Return JSON: {\"anomalyIndices\": [number, number]}" valuesText
                
                let! aiResponse = aiService.AnalyzeTextAsync(prompt, "analytics") |> Async.AwaitTask
                
                let jsonDoc = JsonDocument.Parse(aiResponse)
                let root = jsonDoc.RootElement
                
                let aiAnomalies = 
                    try
                        root.GetProperty("anomalyIndices").EnumerateArray()
                        |> Seq.map (fun i -> i.GetInt32())
                        |> Set.ofSeq
                    with _ -> Set.empty
                
                let avg = DataAggregator.calculateAverage values
                let stdDev = DataAggregator.calculateStandardDeviation values
                
                if stdDev = 0.0 then
                    return []
                else
                    let anomalies = 
                        values
                        |> List.mapi (fun i v ->
                            let zScore = (v - avg) / stdDev
                            let isAnomaly = abs zScore > threshold || aiAnomalies.Contains i
                            if isAnomaly then (i, zScore) else (i, 0.0))
                        |> List.filter (fun (_, z) -> z <> 0.0)
                    
                    return anomalies
        }

    // Predict future trend using AI
    let predictFutureTrend (aiService: IAIService) (values: float list) (steps: int) : Async<float list> =
        async {
            if values.IsEmpty then
                return []
            else
                let valuesText = values |> List.map string |> String.concat ", "
                let prompt = sprintf "Predict next %d values for trend: [%s]. Return JSON: {\"predictions\": [number, number, number]}" steps valuesText
                
                let! aiResponse = aiService.AnalyzeTextAsync(prompt, "analytics") |> Async.AwaitTask
                
                let jsonDoc = JsonDocument.Parse(aiResponse)
                let root = jsonDoc.RootElement
                
                let predictions = 
                    try
                        root.GetProperty("predictions").EnumerateArray()
                        |> Seq.map (fun p -> p.GetDouble())
                        |> List.ofSeq
                    with _ ->
                        // Fallback: simple linear extrapolation
                        let last = values |> List.last
                        let secondLast = values |> List.rev |> List.tail |> List.head
                        let change = last - secondLast
                        [1..steps] |> List.map (fun i -> last + float i * change)
                
                return predictions
        }
