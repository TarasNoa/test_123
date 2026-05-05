using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Trading.Application.Abstractions;
using Libr4.Trading.Application.Dtos;
using MediatR;

namespace Libr4.Trading.Application.MarketData.Queries;

public record GetMarketDataQuery(string Symbol) : IRequest<Result<AssetPriceDto>>;

public class GetMarketDataHandler : IRequestHandler<GetMarketDataQuery, Result<AssetPriceDto>>
{
    private readonly IMarketDataService _marketData;

    public GetMarketDataHandler(IMarketDataService marketData)
    {
        _marketData = marketData;
    }

    public async Task<Result<AssetPriceDto>> Handle(GetMarketDataQuery request, CancellationToken cancellationToken)
    {
        var price = await _marketData.GetPriceAsync(request.Symbol, cancellationToken);
        
        if (price == null)
            return Result.Failure<AssetPriceDto>(Error.NotFound("MarketData.NotFound", $"Price data for {request.Symbol} not found"));

        return Result.Success(price);
    }
}

public record GetTopAssetsQuery(int Limit = 20) : IRequest<Result<List<AssetPriceDto>>>;

public class GetTopAssetsHandler : IRequestHandler<GetTopAssetsQuery, Result<List<AssetPriceDto>>>
{
    private readonly IMarketDataService _marketData;

    public GetTopAssetsHandler(IMarketDataService marketData)
    {
        _marketData = marketData;
    }

    public async Task<Result<List<AssetPriceDto>>> Handle(GetTopAssetsQuery request, CancellationToken cancellationToken)
    {
        var assets = await _marketData.GetTopAssetsAsync(request.Limit, cancellationToken);
        return Result.Success(assets);
    }
}
