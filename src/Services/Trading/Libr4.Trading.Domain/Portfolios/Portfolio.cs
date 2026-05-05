using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Trading.Domain.Portfolios;

public class Portfolio : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<PortfolioPosition> _positions = new();
    public IReadOnlyCollection<PortfolioPosition> Positions => _positions.AsReadOnly();

    private Portfolio() { } // EF Core

    public Portfolio(Guid id, Guid userId, string name, bool isDefault = false) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        UserId = userId;
        Name = name;
        IsDefault = isDefault;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePosition(Guid assetId, string assetSymbol, decimal quantity, decimal averagePrice)
    {
        var position = _positions.FirstOrDefault(p => p.AssetId == assetId);
        
        if (position == null)
        {
            if (quantity != 0)
            {
                _positions.Add(new PortfolioPosition(
                    Guid.NewGuid(),
                    Id,
                    assetId,
                    assetSymbol,
                    quantity,
                    averagePrice));
            }
        }
        else
        {
            if (quantity == 0)
            {
                _positions.Remove(position);
            }
            else
            {
                position.Update(quantity, averagePrice);
            }
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public decimal? GetPositionQuantity(Guid assetId)
    {
        return _positions.FirstOrDefault(p => p.AssetId == assetId)?.Quantity;
    }

    public void SetDefault()
    {
        IsDefault = true;
    }
}

public class PortfolioPosition : Entity<Guid>
{
    public Guid PortfolioId { get; private set; }
    public Guid AssetId { get; private set; }
    public string AssetSymbol { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal AverageEntryPrice { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PortfolioPosition() { } // EF Core

    public PortfolioPosition(Guid id, Guid portfolioId, Guid assetId, string assetSymbol, decimal quantity, decimal averageEntryPrice) : base(id)
    {
        PortfolioId = portfolioId;
        AssetId = assetId;
        AssetSymbol = assetSymbol.ToUpper();
        Quantity = quantity;
        AverageEntryPrice = averageEntryPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(decimal quantity, decimal averagePrice)
    {
        Quantity = quantity;
        AverageEntryPrice = averagePrice;
        UpdatedAt = DateTime.UtcNow;
    }
}
