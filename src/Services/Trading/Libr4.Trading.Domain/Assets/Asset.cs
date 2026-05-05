using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Trading.Domain.Assets;

public enum AssetType
{
    Crypto,
    Stock,
    Forex,
    Commodity
}

public class Asset : Entity<Guid>
{
    public string Symbol { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public AssetType Type { get; private set; }
    public string? Exchange { get; private set; }
    public int Precision { get; private set; } // Decimal places for quantities
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Asset() { } // EF Core

    public Asset(Guid id, string symbol, string name, AssetType type, string? exchange = null, int precision = 8) : base(id)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be empty", nameof(symbol));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Symbol = symbol.ToUpper();
        Name = name;
        Type = type;
        Exchange = exchange;
        Precision = precision;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void UpdatePrice(decimal price, DateTime timestamp)
    {
        // Price updates are handled via MarketData service
        // This is a placeholder for domain logic if needed
    }
}

public class AssetPrice : Entity<Guid>
{
    public Guid AssetId { get; private set; }
    public decimal Price { get; private set; }
    public decimal? Bid { get; private set; }
    public decimal? Ask { get; private set; }
    public decimal? Volume24h { get; private set; }
    public decimal? Change24h { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Source { get; private set; } = string.Empty;

    private AssetPrice() { } // EF Core

    public AssetPrice(Guid id, Guid assetId, decimal price, string source, 
        decimal? bid = null, decimal? ask = null, decimal? volume24h = null, decimal? change24h = null) : base(id)
    {
        AssetId = assetId;
        Price = price;
        Source = source;
        Bid = bid;
        Ask = ask;
        Volume24h = volume24h;
        Change24h = change24h;
        Timestamp = DateTime.UtcNow;
    }
}
