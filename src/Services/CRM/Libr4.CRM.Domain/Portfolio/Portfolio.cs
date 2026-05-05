using Libr4.Shared.Kernel.Domain;

namespace Libr4.CRM.Domain.Portfolio;

public enum PortfolioVisibility
{
    Private,
    Public,
    Restricted
}

public class Portfolio : AggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid OwnerId { get; private set; }
    public PortfolioVisibility Visibility { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<PortfolioItem> _items = new();
    public IReadOnlyCollection<PortfolioItem> Items => _items.AsReadOnly();

    private Portfolio() { }

    public static Portfolio Create(string title, Guid ownerId, string? description = null, PortfolioVisibility visibility = PortfolioVisibility.Private)
    {
        return new Portfolio
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            OwnerId = ownerId,
            Visibility = visibility,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AddItem(PortfolioItem item)
    {
        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _items.Remove(item);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateVisibility(PortfolioVisibility visibility)
    {
        Visibility = visibility;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        Title = title;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class PortfolioItem : Entity<Guid>
{
    public Guid PortfolioId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ProjectUrl { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? Technologies { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public int Order { get; private set; }

    private PortfolioItem() { }

    public static PortfolioItem Create(Guid portfolioId, string title, string? description = null, string? projectUrl = null, string? imageUrl = null, string? technologies = null, int order = 0)
    {
        return new PortfolioItem
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            Title = title,
            Description = description,
            ProjectUrl = projectUrl,
            ImageUrl = imageUrl,
            Technologies = technologies,
            Order = order
        };
    }

    public void MarkAsCompleted(DateTime completedDate)
    {
        CompletedDate = completedDate;
    }

    public void UpdateOrder(int order)
    {
        Order = order;
    }

    public void UpdateDetails(string? description, string? technologies)
    {
        Description = description;
        Technologies = technologies;
    }
}
