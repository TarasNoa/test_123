namespace Libr4.Trading.Domain.MultiExchange.FSharp

open System

type Exchange = Binance | Coinbase | Kraken | Bybit | OKX | Bitfinex | Huobi

type ExchangeFeatures = {
    supportsSpot: bool
    supportsFutures: bool
    supportsMargin: bool
    supportsOptions: bool
    minOrderSize: decimal
    makerFee: decimal
    takerFee: decimal
}

type ExchangeAccount = {
    id: Guid
    userId: Guid
    exchange: Exchange
    apiKey: string
    apiSecret: string
    passphrase: string option
    balance: decimal
    features: ExchangeFeatures
    isActive: bool
    lastSyncedAt: DateTimeOffset
    createdAt: DateTimeOffset
}

type CrossExchangeArbitrage = {
    id: Guid
    symbol: string
    buyExchange: Exchange
    sellExchange: Exchange
    buyPrice: decimal
    sellPrice: decimal
    spreadPercent: decimal
    estimatedProfit: decimal
    detectedAt: DateTimeOffset
}

type ExchangeOrderRouting = {
    id: Guid
    orderId: Guid
    preferredExchanges: Exchange list
    routingStrategy: string
    selectedExchange: Exchange option
    reason: string
}
