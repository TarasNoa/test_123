using Libr4.Trading.Application.Dtos;

namespace Libr4.Trading.Application.Abstractions;

public interface IMarketDataService
{
    Task<AssetPriceDto?> GetPriceAsync(string symbol, CancellationToken cancellationToken = default);
    Task<List<AssetPriceDto>> GetPricesAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default);
    Task<List<AssetPriceDto>> GetTopAssetsAsync(int limit = 100, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AssetPriceDto> SubscribeToPriceUpdatesAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default);
}
