namespace Libr4.Trading.Domain.MultiExchange.FSharp

open System

module ExchangeOps =
    let getBalance (account: ExchangeAccount) = account.balance

    let activateAccount (account: ExchangeAccount) =
        { account with isActive = true }

    let deactivateAccount (account: ExchangeAccount) =
        { account with isActive = false }

    let getFees (exchange: Exchange) : ExchangeFeatures =
        match exchange with
        | Binance -> { supportsSpot = true; supportsFutures = true; supportsMargin = true; supportsOptions = true; minOrderSize = 0.0001m; makerFee = 0.001m; takerFee = 0.001m }
        | Coinbase -> { supportsSpot = true; supportsFutures = false; supportsMargin = false; supportsOptions = false; minOrderSize = 0.001m; makerFee = 0.005m; takerFee = 0.006m }
        | Kraken -> { supportsSpot = true; supportsFutures = true; supportsMargin = true; supportsOptions = false; minOrderSize = 0.0001m; makerFee = 0.0016m; takerFee = 0.0026m }
        | Bybit -> { supportsSpot = true; supportsFutures = true; supportsMargin = true; supportsOptions = true; minOrderSize = 0.001m; makerFee = 0.001m; takerFee = 0.001m }
        | _ -> { supportsSpot = true; supportsFutures = false; supportsMargin = false; supportsOptions = false; minOrderSize = 0.001m; makerFee = 0.002m; takerFee = 0.002m }

module ArbitrageOps =
    let calculateSpread (buyPrice: decimal) (sellPrice: decimal) : decimal =
        if buyPrice > 0m then (sellPrice - buyPrice) / buyPrice * 100m else 0m

    let isProfitable (arb: CrossExchangeArbitrage) (minSpread: decimal) : bool =
        arb.spreadPercent >= minSpread

module RoutingOps =
    let selectBestExchange (routing: ExchangeOrderRouting) (exchanges: Exchange list) : ExchangeOrderRouting =
        let best = routing.preferredExchanges |> List.tryFind (fun e -> List.contains e exchanges)
        { routing with selectedExchange = best; reason = "Best available from preferred list" }
