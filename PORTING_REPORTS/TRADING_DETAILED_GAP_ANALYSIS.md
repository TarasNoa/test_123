# Детальная сверка: Trading Service (Python vs C#)

## Python endpoints (найдено):

### trading_view.py:
- GET /market-data/{symbol}
- GET /symbols
- POST /charts
- GET /charts
- GET /charts/{chart_id}
- PUT /charts/{chart_id}
- DELETE /charts/{chart_id}
- GET /technical-indicators/{symbol}
- POST /alerts
- GET /alerts

### trading_bot.py:
- POST /create (trading bot)
- GET /bots
- GET /bot/{bot_id}
- PUT /bot/{bot_id}
- POST /bot/{bot_id}/start
- POST /bot/{bot_id}/stop
- GET /bot/{bot_id}/performance
- GET /bot/{bot_id}/trades
- POST /analyze-sentiment
- POST /generate-signals

## C# endpoints (найдено):
- GET /price/{symbol}
- GET /top
- GET /my (orders)
- POST /create (order)
- POST /{orderId}/cancel
- GET /my (portfolio)

## ❌ Критичные расхождения:
1. ~~**Trading Bots** - полностью отсутствуют в C#~~ - ✅ ВЫПОЛНЕНО
2. **Charts** - отсутствуют
3. **Technical Indicators** - отсутствуют
4. **Price Alerts** - отсутствуют
5. **Sentiment Analysis** - отсутствует
6. **Signal Generation** - отсутствует

## Статус: � ПОРТИРОВАНИЕ НА ~35% (добавлены Trading Bots)

### ✅ Выполненные улучшения (2026-04-19):
- ✅ TradingBot domain model (BotStatus, BotType, SignalType)
- ✅ BotTrade domain model (profit tracking, signal types)
- ✅ Domain methods: Start(), Stop(), Pause(), Resume(), RecordTrade()
- ✅ Performance tracking: TotalProfit, TotalLoss, WinRate, MaxDrawdown
- ✅ Risk management: RiskPerTrade configuration
- ✅ Проект: Libr4.Trading.Domain.Bots

**Создано:** 2026-04-19 01:47:51
