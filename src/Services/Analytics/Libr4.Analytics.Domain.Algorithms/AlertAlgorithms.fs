namespace Libr4.Analytics.Domain.Algorithms

open System

[<CLIMutable>]
type MetricData =
    { Name: string
      Value: float
      Timestamp: DateTimeOffset
      Labels: Map<string, string> }

[<CLIMutable>]
type AlertCondition =
    { MetricName: string
      Operator: string // ">", "<", "==", "!="
      Threshold: float }

module AlertAlgorithms =

    let private parseCondition (condition: string) =
        // Simple parser for conditions like "cpu > 80"
        let parts = condition.Split(' ')
        if parts.Length = 3 then
            Some { MetricName = parts.[0]; Operator = parts.[1]; Threshold = float parts.[2] }
        else None

    let evaluateCondition (condition: string) (metric: MetricData) =
        match parseCondition condition with
        | Some cond when cond.MetricName = metric.Name ->
            match cond.Operator with
            | ">" -> metric.Value > cond.Threshold
            | "<" -> metric.Value < cond.Threshold
            | "==" -> metric.Value = cond.Threshold
            | "!=" -> metric.Value <> cond.Threshold
            | _ -> false
        | _ -> false

    let calculateMovingAverage (values: float list) (windowSize: int) =
        if values.Length < windowSize then []
        else
            values
            |> List.windowed windowSize
            |> List.map (fun window -> window |> List.average)

    let detectAnomalies (values: float list) (threshold: float) =
        let mean = values |> List.average
        let stdDev = values |> List.map (fun v -> (v - mean) ** 2.0) |> List.average |> sqrt
        values |> List.filter (fun v -> abs (v - mean) > threshold * stdDev)
