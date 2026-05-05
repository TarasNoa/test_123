using Libr4.Shared.Kernel.Domain;

namespace Libr4.CRM.Domain.CRM;

public enum CustomerStatus
{
    Lead,
    Prospect,
    Active,
    Inactive,
    Churned
}

public enum DealStage
{
    Qualification,
    Proposal,
    Negotiation,
    ClosedWon,
    ClosedLost
}

public class Customer : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Company { get; private set; }
    public CustomerStatus Status { get; private set; }
    public decimal LifetimeValue { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<Deal> _deals = new();
    public IReadOnlyCollection<Deal> Deals => _deals.AsReadOnly();

    private readonly List<CustomerInteraction> _interactions = new();
    public IReadOnlyCollection<CustomerInteraction> Interactions => _interactions.AsReadOnly();

    private Customer() { }

    public static Customer Create(string name, string? email = null, string? phone = null, string? company = null)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Phone = phone,
            Company = company,
            Status = CustomerStatus.Lead,
            LifetimeValue = 0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateStatus(CustomerStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddDeal(Deal deal)
    {
        _deals.Add(deal);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddInteraction(CustomerInteraction interaction)
    {
        _interactions.Add(interaction);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLifetimeValue(decimal value)
    {
        LifetimeValue = value;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class Deal : Entity<Guid>
{
    public Guid CustomerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Value { get; private set; }
    public DealStage Stage { get; private set; }
    public DateTime ExpectedCloseDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Deal() { }

    public static Deal Create(Guid customerId, string title, decimal value, DateTime expectedCloseDate, string? description = null)
    {
        return new Deal
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = title,
            Description = description,
            Value = value,
            Stage = DealStage.Qualification,
            ExpectedCloseDate = expectedCloseDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateStage(DealStage stage)
    {
        Stage = stage;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateValue(decimal value)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class CustomerInteraction : Entity<Guid>
{
    public Guid CustomerId { get; private set; }
    public Guid? DealId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTime InteractionDate { get; private set; }
    public Guid? PerformedByUserId { get; private set; }

    private CustomerInteraction() { }

    public static CustomerInteraction Create(Guid customerId, string type, string? notes = null, Guid? dealId = null, Guid? performedByUserId = null)
    {
        return new CustomerInteraction
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            DealId = dealId,
            Type = type,
            Notes = notes,
            InteractionDate = DateTime.UtcNow,
            PerformedByUserId = performedByUserId
        };
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }
}
