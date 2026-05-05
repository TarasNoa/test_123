namespace Libr4.Trading.Domain.RiskScoring

open System

// Types (must be defined before use)
type SignalStrength = None | Weak | Moderate | Strong
type TrendDirection = Bullish | Bearish | Neutral

type TradePerformance = {
    WinRate: float
    TotalProfit: decimal
    TotalLoss: decimal
    ProfitFactor: float
    WinCount: int
    LossCount: int
    TotalTrades: int
}

// Risk Scoring Algorithms
module RiskCalculator =

    // Calculate position size based on risk percentage and account balance
    let calculatePositionSize (accountBalance: decimal) (riskPercentage: float) (entryPrice: decimal) (stopLossPrice: decimal) : int =
        let riskAmount = accountBalance * decimal riskPercentage
        let priceDifference = abs (entryPrice - stopLossPrice)
        if priceDifference = 0m then 0
        else int (riskAmount / priceDifference)

    // Calculate risk/reward ratio
    let calculateRiskRewardRatio (entryPrice: decimal) (targetPrice: decimal) (stopLossPrice: decimal) : float =
        let potentialProfit = abs (targetPrice - entryPrice)
        let potentialLoss = abs (stopLossPrice - entryPrice)
        if potentialLoss = 0m then 0.0
        else float (potentialProfit / potentialLoss)

    // Calculate position value
    let calculatePositionValue (positionSize: int) (entryPrice: decimal) : decimal =
        decimal positionSize * entryPrice

    // Calculate maximum drawdown from a series of equity values
    let calculateMaxDrawdown (equityValues: decimal list) : float =
        match equityValues with
        | [] -> 0.0
        | _ ->
            let peak = equityValues |> List.max
            let trough = equityValues |> List.min
            if peak = 0m then 0.0
            else float ((peak - trough) / peak) * 100.0

    // Calculate win rate from trade results
    let calculateWinRate (winCount: int) (totalTrades: int) : float =
        if totalTrades = 0 then 0.0
        else float (winCount * 100 / totalTrades)

    // Calculate Kelly Criterion for optimal position sizing
    let calculateKellyCriterion (winRate: float) (averageWin: decimal) (averageLoss: decimal) : float =
        if averageLoss = 0m then 0.0
        else
            let winLossRatio = float (averageWin / averageLoss)
            let kelly = (winRate * winLossRatio - (1.0 - winRate)) / winLossRatio
            max 0.0 kelly // Never bet more than 100%

// Trade Analysis Algorithms
module TradeAnalyzer =

    // Analyze trade performance
    let analyzeTradePerformance (trades: (bool * decimal) list) : TradePerformance =
        let winningTrades = trades |> List.filter fst
        let losingTrades = trades |> List.filter (fst >> not)
        
        let winCount = List.length winningTrades
        let lossCount = List.length losingTrades
        let totalTrades = List.length trades
        
        let totalProfit = winningTrades |> List.sumBy snd
        let totalLoss = losingTrades |> List.sumBy (abs << snd)
        
        let winRate = RiskCalculator.calculateWinRate winCount totalTrades
        let profitFactor = if totalLoss = 0m then 0.0m else totalProfit / totalLoss
        
        {
            WinRate = winRate
            TotalProfit = totalProfit
            TotalLoss = totalLoss
            ProfitFactor = float profitFactor
            WinCount = winCount
            LossCount = lossCount
            TotalTrades = totalTrades
        }

    // Calculate Sharpe Ratio
    let calculateSharpeRatio (returns: float list) (riskFreeRate: float) : float =
        match returns with
        | [] -> 0.0
        | _ ->
            let averageReturn = List.average returns
            let variance = List.averageBy (fun r -> (r - averageReturn) ** 2.0) returns
            let stdDev = sqrt variance
            if stdDev = 0.0 then 0.0
            else (averageReturn - riskFreeRate) / stdDev

    // Calculate Sortino Ratio (downside risk only)
    let calculateSortinoRatio (returns: float list) (riskFreeRate: float) : float =
        match returns with
        | [] -> 0.0
        | _ ->
            let averageReturn = List.average returns
            let downsideReturns = returns |> List.filter (fun r -> r < riskFreeRate)
            if List.isEmpty downsideReturns then 0.0
            else
                let downsideVariance = 
                    downsideReturns 
                    |> List.averageBy (fun r -> (r - riskFreeRate) ** 2.0)
                let downsideDev = sqrt downsideVariance
                if downsideDev = 0.0 then 0.0
                else (averageReturn - riskFreeRate) / downsideDev

// Signal Analysis Algorithms
module SignalAnalyzer =

    // Analyze signal strength based on multiple factors
    let analyzeSignalStrength (rsi: float) (macd: float) (volume: float) (priceAction: float) : SignalStrength =
        let score = 
            (if rsi < 30.0 || rsi > 70.0 then 1.0 else 0.0) +
            (if abs macd > 0.5 then 1.0 else 0.0) +
            (if volume > 1.5 then 1.0 else 0.0) +
            (if priceAction > 0.7 then 1.0 else 0.0)
        
        match score with
        | s when s >= 3.0 -> Strong
        | s when s >= 2.0 -> Moderate
        | s when s >= 1.0 -> Weak
        | _ -> None

    // Validate signal against market conditions
    let validateSignal (signal: SignalStrength) (marketVolatility: float) (trendDirection: TrendDirection) : bool =
        match signal with
        | None -> false
        | Weak -> marketVolatility < 0.3
        | Moderate -> marketVolatility < 0.5
        | Strong -> true
