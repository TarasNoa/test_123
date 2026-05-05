using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.CRM;

public sealed class CRMAccount : AggregateRoot<Guid>
{
    public Guid OwnerId { get; private set; }
    public string CompanyName { get; private set; } = "";
    public string? Industry { get; private set; }
    public string? CompanySize { get; private set; }
    public string? Website { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? Description { get; private set; }
    public int? FoundedYear { get; private set; }
    public decimal? Revenue { get; private set; }
    public int? Employees { get; private set; }
    public string SubscriptionPlan { get; private set; } = "professional";
    public Dictionary<string, object> AiConfiguration { get; private set; } = new();
    public Dictionary<string, object> AutomationSettings { get; private set; } = new();
    public string Status { get; private set; } = "active";
    public bool IsVerified { get; private set; }
    public int ContactsCount { get; private set; }
    public int DealsCount { get; private set; }
    public int TasksCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<CRMContact> _contacts = new();
    private readonly List<CRMDeal> _deals = new();
    private readonly List<CRMTask> _tasks = new();
    private readonly List<CRMActivity> _activities = new();
    private readonly List<CRMPipeline> _pipelines = new();

    public IReadOnlyCollection<CRMContact> Contacts => _contacts.AsReadOnly();
    public IReadOnlyCollection<CRMDeal> Deals => _deals.AsReadOnly();
    public IReadOnlyCollection<CRMTask> Tasks => _tasks.AsReadOnly();
    public IReadOnlyCollection<CRMActivity> Activities => _activities.AsReadOnly();
    public IReadOnlyCollection<CRMPipeline> Pipelines => _pipelines.AsReadOnly();

    private CRMAccount() { }

    public static CRMAccount Create(
        Guid ownerId,
        string companyName,
        string? industry,
        string? companySize,
        string? website,
        string? phone,
        string? email,
        string? address,
        string? description,
        DateTimeOffset now)
    {
        return new CRMAccount
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            CompanyName = companyName.Trim(),
            Industry = industry?.Trim(),
            CompanySize = companySize?.Trim(),
            Website = website?.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            Description = description?.Trim(),
            SubscriptionPlan = "professional",
            Status = "active",
            IsVerified = false,
            ContactsCount = 0,
            DealsCount = 0,
            TasksCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateCompanyInfo(
        string? industry,
        string? companySize,
        string? website,
        string? phone,
        string? email,
        string? address,
        string? description,
        int? foundedYear,
        decimal? revenue,
        int? employees,
        DateTimeOffset now)
    {
        Industry = industry?.Trim();
        CompanySize = companySize?.Trim();
        Website = website?.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        Description = description?.Trim();
        FoundedYear = foundedYear;
        Revenue = revenue;
        Employees = employees;
        UpdatedAt = now;
    }

    public void UpdateSubscriptionPlan(string plan, DateTimeOffset now)
    {
        SubscriptionPlan = plan.Trim();
        UpdatedAt = now;
    }

    public void Verify(DateTimeOffset now)
    {
        IsVerified = true;
        UpdatedAt = now;
    }

    public void UpdateAiConfiguration(Dictionary<string, object> config, DateTimeOffset now)
    {
        AiConfiguration = config ?? new();
        UpdatedAt = now;
    }

    public void UpdateAutomationSettings(Dictionary<string, object> settings, DateTimeOffset now)
    {
        AutomationSettings = settings ?? new();
        UpdatedAt = now;
    }

    public void IncrementContactsCount(DateTimeOffset now)
    {
        ContactsCount++;
        UpdatedAt = now;
    }

    public void DecrementContactsCount(DateTimeOffset now)
    {
        if (ContactsCount > 0)
            ContactsCount--;
        UpdatedAt = now;
    }

    public void IncrementDealsCount(DateTimeOffset now)
    {
        DealsCount++;
        UpdatedAt = now;
    }

    public void DecrementDealsCount(DateTimeOffset now)
    {
        if (DealsCount > 0)
            DealsCount--;
        UpdatedAt = now;
    }

    public void IncrementTasksCount(DateTimeOffset now)
    {
        TasksCount++;
        UpdatedAt = now;
    }

    public void DecrementTasksCount(DateTimeOffset now)
    {
        if (TasksCount > 0)
            TasksCount--;
        UpdatedAt = now;
    }

    public double GetUtilizationRate()
    {
        var limits = new Dictionary<string, (int contacts, int deals, int tasks)>
        {
            { "starter", (1000, 100, 500) },
            { "professional", (10000, 1000, 5000) },
            { "enterprise", (100000, 10000, 50000) }
        };

        var (contactLimit, dealLimit, taskLimit) = limits.ContainsKey(SubscriptionPlan)
            ? limits[SubscriptionPlan]
            : limits["professional"];

        var contactUtil = (ContactsCount / (double)contactLimit) * 100;
        var dealUtil = (DealsCount / (double)dealLimit) * 100;
        var taskUtil = (TasksCount / (double)taskLimit) * 100;

        return Math.Max(Math.Max(contactUtil, dealUtil), taskUtil);
    }

    public bool IsEnterprise => SubscriptionPlan is "enterprise" or "custom";
}

public sealed class CRMContact
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AccountId { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Mobile { get; private set; }
    public string? Company { get; private set; }
    public string? JobTitle { get; private set; }
    public string? Department { get; private set; }
    public string? LinkedIn { get; private set; }
    public string? Street { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Country { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Notes { get; private set; }
    public List<string> Tags { get; private set; } = new();
    public Dictionary<string, object> CustomFields { get; private set; } = new();
    public int LeadScore { get; private set; }
    public string? LeadSource { get; private set; }
    public string? LeadStatus { get; private set; }
    public string? PreferredContactMethod { get; private set; }
    public bool DoNotCall { get; private set; }
    public bool DoNotEmail { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? LastContactedAt { get; private set; }

    private CRMContact() { }

    internal static CRMContact Create(
        Guid accountId,
        string? firstName,
        string? lastName,
        string? email,
        string? phone,
        DateTimeOffset now)
    {
        return new CRMContact
        {
            AccountId = accountId,
            FirstName = firstName?.Trim(),
            LastName = lastName?.Trim(),
            Email = email?.Trim(),
            Phone = phone?.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public string GetFullName() => $"{FirstName} {LastName}".Trim();
    public string GetDisplayName() => GetFullName() != "" ? GetFullName() : Company ?? "Unknown";
}

public sealed class CRMDeal
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AccountId { get; private set; }
    public Guid? ContactId { get; private set; }
    public string DealName { get; private set; } = "";
    public string? Description { get; private set; }
    public decimal? Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public int? Probability { get; private set; }
    public decimal? WeightedAmount { get; private set; }
    public DealStage Stage { get; private set; }
    public Guid? PipelineId { get; private set; }
    public int? StageOrder { get; private set; }
    public DateTimeOffset? ExpectedCloseDate { get; private set; }
    public DateTimeOffset? ActualCloseDate { get; private set; }
    public List<string> Competitors { get; private set; } = new();
    public string? NextSteps { get; private set; }
    public string? LossReason { get; private set; }
    public Dictionary<string, object> CustomFields { get; private set; } = new();
    public bool IsActive { get; private set; }
    public bool IsWon { get; private set; }
    public bool IsLost { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? LastActivityAt { get; private set; }

    private CRMDeal() { }

    internal static CRMDeal Create(
        Guid accountId,
        Guid? contactId,
        string dealName,
        string? description,
        DateTimeOffset now)
    {
        return new CRMDeal
        {
            AccountId = accountId,
            ContactId = contactId,
            DealName = dealName.Trim(),
            Description = description?.Trim(),
            Stage = DealStage.Lead,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateStage(DealStage stage, DateTimeOffset now)
    {
        Stage = stage;
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void MarkWon(DateTimeOffset now)
    {
        IsWon = true;
        IsActive = false;
        Stage = DealStage.ClosedWon;
        ActualCloseDate = now;
        UpdatedAt = now;
    }

    public void MarkLost(string? reason, DateTimeOffset now)
    {
        IsLost = true;
        IsActive = false;
        Stage = DealStage.ClosedLost;
        LossReason = reason?.Trim();
        ActualCloseDate = now;
        UpdatedAt = now;
    }
}

public sealed class CRMTask
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AccountId { get; private set; }
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public TaskStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public Guid? AssignedTo { get; private set; }
    public string TaskType { get; private set; } = "manual";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private CRMTask() { }

    internal static CRMTask Create(
        Guid accountId,
        string title,
        string? description,
        TaskPriority priority,
        DateTimeOffset? dueDate,
        Guid? assignedTo,
        DateTimeOffset now)
    {
        return new CRMTask
        {
            AccountId = accountId,
            Title = title.Trim(),
            Description = description?.Trim(),
            Priority = priority,
            DueDate = dueDate,
            AssignedTo = assignedTo,
            Status = TaskStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

public sealed class CRMActivity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AccountId { get; private set; }
    public Guid? ContactId { get; private set; }
    public Guid? DealId { get; private set; }
    public ActivityType ActivityType { get; private set; }
    public string? Subject { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset ActivityDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private CRMActivity() { }

    internal static CRMActivity Create(
        Guid accountId,
        Guid? contactId,
        Guid? dealId,
        ActivityType activityType,
        string? subject,
        string? description,
        DateTimeOffset activityDate,
        DateTimeOffset now)
    {
        return new CRMActivity
        {
            AccountId = accountId,
            ContactId = contactId,
            DealId = dealId,
            ActivityType = activityType,
            Subject = subject?.Trim(),
            Description = description?.Trim(),
            ActivityDate = activityDate,
            CreatedAt = now
        };
    }
}

public sealed class CRMPipeline
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AccountId { get; private set; }
    public string Name { get; private set; } = "";
    public string? Description { get; private set; }
    public List<string> Stages { get; private set; } = new();
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public bool AutoAdvance { get; private set; }
    public int TotalDeals { get; private set; }
    public decimal TotalValue { get; private set; }
    public double? ConversionRate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private CRMPipeline() { }

    internal static CRMPipeline Create(
        Guid accountId,
        string name,
        string? description,
        List<string> stages,
        DateTimeOffset now)
    {
        return new CRMPipeline
        {
            AccountId = accountId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Stages = stages ?? new(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

public enum TaskStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
    Overdue = 4
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum DealStage
{
    Lead = 0,
    Qualified = 1,
    Proposal = 2,
    Negotiation = 3,
    ClosedWon = 4,
    ClosedLost = 5
}

public enum ActivityType
{
    Call = 0,
    Email = 1,
    Meeting = 2,
    Task = 3,
    Note = 4,
    Demo = 5,
    FollowUp = 6
}
