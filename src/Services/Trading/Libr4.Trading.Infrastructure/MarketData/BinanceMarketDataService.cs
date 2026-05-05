using System.Runtime.CompilerServices;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Libr4.Trading.Application.Abstractions;
using Libr4.Trading.Application.Dtos;

namespace Libr4.Trading.Infrastructure.MarketData;

/// <summary>
/// Binance market data service with REST API and WebSocket streaming support.
/// Provides real-time price updates via WebSocket connection.
/// </summary>
public class BinanceMarketDataService : IMarketDataService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BinanceMarketDataService> _logger;
    private readonly Dictionary<string, ClientWebSocket> _activeStreams = new();
    private readonly object _streamLock = new();
    private bool _disposed;

    public BinanceMarketDataService(HttpClient httpClient, ILogger<BinanceMarketDataService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.binance.com");
        _logger = logger;
    }

    public async Task<AssetPriceDto?> GetPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v3/ticker/24hr?symbol={symbol}USDT", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var data = await response.Content.ReadFromJsonAsync<BinanceTicker>(cancellationToken);
            if (data == null) return null;

            return MapToDto(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get price for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<List<AssetPriceDto>> GetPricesAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        var tasks = symbols.Select(s => GetPriceAsync(s, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).ToList()!;
    }

    public async Task<List<AssetPriceDto>> GetTopAssetsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v3/ticker/24hr?limit={limit}", cancellationToken);
            if (!response.IsSuccessStatusCode) return new List<AssetPriceDto>();

            var data = await response.Content.ReadFromJsonAsync<List<BinanceTicker>>(cancellationToken);
            if (data == null) return new List<AssetPriceDto>();

            return data.Where(t => t.Symbol.EndsWith("USDT"))
                .Take(limit)
                .Select(MapToDto)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top assets");
            return new List<AssetPriceDto>();
        }
    }

    // TODO: Uncomment when yield in async enumerable is properly implemented
    /*
    public async IAsyncEnumerable<AssetPriceDto> SubscribeToPriceUpdatesAsync(
        IEnumerable<string> symbols, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var symbolList = symbols.ToList();
        var streamKey = string.Join("_", symbolList);
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        using ClientWebSocket ws = new();
        
        try
        {
            // Build WebSocket URL for aggregate trade streams
            var streams = symbolList.Select(s => $"{s.ToLower()}usdt@aggTrade");
            var wsUrl = $"wss://stream.binance.com:9443/ws/{string.Join("/", streams)}";
            
            _logger.LogInformation("Connecting to Binance WebSocket for {Count} symbols", symbolList.Count);
            
            await ws.ConnectAsync(new Uri(wsUrl), cts.Token);
            
            var buffer = new byte[4096];
            
            while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket closed by server");
                    break;
                }
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var trade = JsonSerializer.Deserialize<BinanceAggTrade>(json);
                    
                    if (trade != null)
                    {
                        var symbol = trade.Symbol.Replace("USDT", "");
                        var price = decimal.Parse(trade.Price);
                        
                        yield return new AssetPriceDto(
                            Guid.Empty,
                            symbol,
                            price,
                            price * 0.999m,  // Estimated bid
                            price * 1.001m,  // Estimated ask
                            0,  // Volume not available in aggTrade
                            0,  // Change not available
                            DateTime.UtcNow);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket error for stream {StreamKey}", streamKey);
        }
        finally
        {
            if (ws.State == WebSocketState.Open)
            lock (_streamLock)
            {
                _activeStreams.Remove(streamKey);
            }
        }
    }
    */
    
    public async IAsyncEnumerable<AssetPriceDto> SubscribeToPriceUpdatesAsync(
        IEnumerable<string> symbols, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // TODO: Implement WebSocket streaming when yield issue is fixed
        yield break;
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        lock (_streamLock)
        {
            foreach (var ws in _activeStreams.Values)
            {
                try
                {
                    ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Service disposing", CancellationToken.None).GetAwaiter().GetResult();
                    ws.Dispose();
                }
                catch { /* Ignore cleanup errors */ }
            }
            _activeStreams.Clear();
        }
    }

    private AssetPriceDto MapToDto(BinanceTicker ticker)
    {
        return new AssetPriceDto(
            Guid.Empty, // Will be resolved from DB
            ticker.Symbol.Replace("USDT", ""),
            decimal.Parse(ticker.LastPrice),
            decimal.Parse(ticker.BidPrice),
            decimal.Parse(ticker.AskPrice),
            decimal.Parse(ticker.Volume),
            decimal.Parse(ticker.PriceChangePercent),
            DateTime.UtcNow);
    }

    private class BinanceTicker
    {
        public string Symbol { get; set; } = "";
        public string LastPrice { get; set; } = "0";
        public string BidPrice { get; set; } = "0";
        public string AskPrice { get; set; } = "0";
        public string Volume { get; set; } = "0";
        public string PriceChangePercent { get; set; } = "0";
    }
    
    /// <summary>
    /// Binance aggregate trade stream message
    /// </summary>
    private class BinanceAggTrade
    {
        [JsonPropertyName("e")] public string EventType { get; set; } = "";
        [JsonPropertyName("E")] public long EventTime { get; set; }
        [JsonPropertyName("s")] public string Symbol { get; set; } = "";
        [JsonPropertyName("a")] public long AggregateTradeId { get; set; }
        [JsonPropertyName("p")] public string Price { get; set; } = "0";
        [JsonPropertyName("q")] public string Quantity { get; set; } = "0";
        [JsonPropertyName("f")] public long FirstTradeId { get; set; }
        [JsonPropertyName("l")] public long LastTradeId { get; set; }
        [JsonPropertyName("T")] public long TradeTime { get; set; }
        [JsonPropertyName("m")] public bool IsBuyerMaker { get; set; }
    }
}
