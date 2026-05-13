namespace Libr4.Trading.Domain.ChartAnalysis.Algorithms

open System
open System.Text.Json
open Libr4.Trading.Domain.ChartAnalysis
open Libr4.AI.Application.Abstractions

// Indicator Calculator
module IndicatorCalculator =

    type IndicatorResult = {
        Value: decimal
        Signal: string option
        Confidence: float32 option
    }

    // Calculate Simple Moving Average (SMA)
    let calculateSMA (prices: decimal list) (period: int) : IndicatorResult =
        if prices.Length < period then
            { Value = 0m; Signal = None; Confidence = None }
        else
            let recent = prices |> List.take period |> List.average
            let signal = 
                if recent > prices.[prices.Length - period] then Some "buy"
                elif recent < prices.[prices.Length - period] then Some "sell"
                else Some "neutral"
            { Value = recent; Signal = signal; Confidence = Some 0.7f }

    // Calculate Relative Strength Index (RSI)
    let calculateRSI (prices: decimal list) (period: int) : IndicatorResult =
        if prices.Length < period + 1 then
            { Value = 50m; Signal = None; Confidence = None }
        else
            let gains = 
                prices 
                |> List.pairwise 
                |> List.map (fun (prev, curr) -> if curr > prev then curr - prev else 0m)
                |> List.take period
                |> List.average
            
            let losses = 
                prices 
                |> List.pairwise 
                |> List.map (fun (prev, curr) -> if curr < prev then prev - curr else 0m)
                |> List.take period
                |> List.average
            
            let rs = if losses = 0m then 100m else gains / losses
            let rsi = 100m - (100m / (1m + rs))
            
            let signal = 
                if rsi < 30m then Some "buy"
                elif rsi > 70m then Some "sell"
                else Some "neutral"
            
            { Value = rsi; Signal = signal; Confidence = Some 0.8f }

    // Generate trading signals using AI
    let generateTradingSignal (aiService: IAIService) (prices: decimal list) (marketContext: string) : Async<IndicatorResult> =
        async {
            let pricesText = prices |> List.map string |> String.concat ", "
            let prompt = sprintf "Analyze price data [%s] with context '%s'. Return JSON: {\"signal\": \"buy/sell/neutral\", \"confidence\": number (0-1)}" pricesText marketContext
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "trading") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let signal = 
                try root.GetProperty("signal").GetString()
                with _ -> "neutral"
            
            let confidence = 
                try root.GetProperty("confidence").GetSingle()
                with _ -> 0.5f
            
            let avgPrice = if prices.IsEmpty then 0m else prices |> List.average
            
            return {
                Value = avgPrice
                Signal = Some signal
                Confidence = Some confidence
            }
        }

// Pattern Recognizer
module PatternRecognizer =

    type PatternDetection = {
        PatternType: PatternType
        Confidence: float32
        Description: string
        TargetPrice: decimal option
        StopLossPrice: decimal option
    }

    // Detect Head and Shoulders pattern
    let detectHeadAndShoulders (prices: decimal list) : PatternDetection option =
        if prices.Length < 5 then None
        else
            let recent = prices |> List.take 5 |> List.toArray
            let leftShoulder = recent.[0]
            let head = recent.[1]
            let rightShoulder = recent.[2]
            
            if head > leftShoulder && head > rightShoulder && 
               abs(float (leftShoulder - rightShoulder)) / float head < 0.05 then
                Some {
                    PatternType = PatternType.HeadAndShoulders
                    Confidence = 0.75f
                    Description = "Head and Shoulders reversal pattern detected"
                    TargetPrice = Some (head - (head - leftShoulder) * 2m)
                    StopLossPrice = Some (head + (head - leftShoulder) * 0.1m)
                }
            else None

    // Detect Double Top pattern
    let detectDoubleTop (prices: decimal list) : PatternDetection option =
        if prices.Length < 4 then None
        else
            let recent = prices |> List.take 4 |> List.toArray
            let firstTop = recent.[0]
            let secondTop = recent.[2]
            
            if abs(float (firstTop - secondTop)) / float firstTop < 0.02 then
                Some {
                    PatternType = PatternType.DoubleTop
                    Confidence = 0.7f
                    Description = "Double Top reversal pattern detected"
                    TargetPrice = Some (firstTop - (firstTop - recent.[1]) * 1.5m)
                    StopLossPrice = Some (firstTop + (firstTop - recent.[1]) * 0.1m)
                }
            else None

    // Detect patterns using AI
    let detectPatternsWithAI (aiService: IAIService) (prices: decimal list) (timeframe: string) : Async<PatternDetection list> =
        async {
            let pricesText = prices |> List.map string |> String.concat ", "
            let prompt = sprintf "Detect chart patterns in price data [%s] for %s timeframe. Return JSON: {\"patterns\": [{\"pattern\": string, \"confidence\": number (0-1), \"description\": string}]}" pricesText timeframe
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "trading") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let patterns = 
                try
                    root.GetProperty("patterns").EnumerateArray()
                    |> Seq.map (fun p -> 
                        {
                            PatternType = PatternType.Triangle
                            Confidence = p.GetProperty("confidence").GetSingle()
                            Description = p.GetProperty("description").GetString()
                            TargetPrice = None
                            StopLossPrice = None
                        })
                    |> List.ofSeq
                with _ ->
                    []
            
            return patterns
        }

// Trend Analyzer
module TrendAnalyzer =

    type TrendAnalysis = {
        Trend: Trend
        BullishScore: float32
        BearishScore: float32
        Strength: string // weak, moderate, strong
    }

    // Analyze overall market trend
    let analyzeTrend (prices: decimal list) (indicators: IndicatorCalculator.IndicatorResult list) : TrendAnalysis =
        if prices.IsEmpty then
            {
                Trend = Trend.Sideways
                BullishScore = 0f
                BearishScore = 0f
                Strength = "neutral"
            }
        else
            let recent = prices |> List.take 10
            let priceChange = (recent |> List.last) - (recent |> List.head)
            let priceChangePercent = float (priceChange / (recent |> List.head)) * 100.0
            
            let bullishSignals = indicators |> List.filter (fun i -> i.Signal = Some "buy") |> List.length
            let bearishSignals = indicators |> List.filter (fun i -> i.Signal = Some "sell") |> List.length
            let totalSignals = indicators.Length |> max 1
            
            let bullishScore = (float32 bullishSignals / float32 totalSignals) * 100f + (if priceChangePercent > 0.0 then float32 priceChangePercent else 0f) |> min 100f
            let bearishScore = (float32 bearishSignals / float32 totalSignals) * 100f + (if priceChangePercent < 0.0 then float32 (abs priceChangePercent) else 0f) |> min 100f
            
            let trend = 
                if bullishScore > bearishScore + 10f then Trend.Bullish
                elif bearishScore > bullishScore + 10f then Trend.Bearish
                else Trend.Sideways
            
            let strength = 
                let maxScore = max bullishScore bearishScore
                if maxScore > 70f then "strong"
                elif maxScore > 50f then "moderate"
                else "weak"
            
            {
                Trend = trend
                BullishScore = bullishScore
                BearishScore = bearishScore
                Strength = strength
            }

    // Analyze trend using AI
    let analyzeTrendWithAI (aiService: IAIService) (prices: decimal list) (volume: int64 list) (marketSentiment: string) : Async<TrendAnalysis> =
        async {
            let pricesText = prices |> List.map string |> String.concat ", "
            let volumeText = volume |> List.map string |> String.concat ", "
            let prompt = sprintf "Analyze market trend: prices [%s], volume [%s], sentiment '%s'. Return JSON: {\"trend\": \"bullish/bearish/sideways\", \"bullishScore\": number (0-100), \"bearishScore\": number (0-100), \"strength\": \"weak/moderate/strong\"}" pricesText volumeText marketSentiment
            
            let! aiResponse = aiService.AnalyzeTextAsync(prompt, "trading") |> Async.AwaitTask
            
            let jsonDoc = JsonDocument.Parse(aiResponse)
            let root = jsonDoc.RootElement
            
            let trendStr = 
                try root.GetProperty("trend").GetString()
                with _ -> "sideways"
            
            let bullishScore = 
                try root.GetProperty("bullishScore").GetSingle()
                with _ -> 50f
            
            let bearishScore = 
                try root.GetProperty("bearishScore").GetSingle()
                with _ -> 50f
            
            let strength = 
                try root.GetProperty("strength").GetString()
                with _ -> "neutral"
            
            let trend = 
                match trendStr with
                | "bullish" -> Trend.Bullish
                | "bearish" -> Trend.Bearish
                | _ -> Trend.Sideways
            
            return {
                Trend = trend
                BullishScore = bullishScore
                BearishScore = bearishScore
                Strength = strength
            }
        }
