using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.Tasks;

public sealed class TaskAggregate : AggregateRoot<Guid>
{
    private readonly List<Application> _applications = new();

    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public TaskCategory Category { get; private set; }
    public TaskStatus Status { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid? AssignedFreelancerId { get; private set; }
    public decimal Budget { get; private set; }
    public string Currency { get; private set; } = "USD";
    public DateTimeOffset? Deadline { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyCollection<Application> Applications => _applications.AsReadOnly();

    private TaskAggregate() { }

    public static TaskAggregate Create(
        string title,
        string description,
        TaskCategory category,
        Guid clientId,
        decimal budget,
        string currency,
        DateTimeOffset? deadline,
        DateTimeOffset now)
    {
        var task = new TaskAggregate
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description.Trim(),
            Category = category,
            Status = TaskStatus.Draft,
            ClientId = clientId,
            Budget = budget,
            Currency = currency.ToUpperInvariant(),
            Deadline = deadline,
            CreatedAt = now,
            UpdatedAt = now
        };

        return task;
    }

    public void Publish(DateTimeOffset now)
    {
        if (Status != TaskStatus.Draft)
            throw new InvalidOperationException("Only draft tasks can be published");

        Status = TaskStatus.Published;
        PublishedAt = now;
        UpdatedAt = now;

        RaiseDomainEvent(new TaskPublishedDomainEvent(Id, ClientId, Title, Budget, Currency));
    }

    public void Update(string title, string description, TaskCategory category, decimal budget, string currency, DateTimeOffset? deadline, DateTimeOffset now)
    {
        if (Status != TaskStatus.Draft)
            throw new InvalidOperationException("Only draft tasks can be updated");

        Title = title.Trim();
        Description = description.Trim();
        Category = category;
        Budget = budget;
        Currency = currency.ToUpperInvariant();
        Deadline = deadline;
        UpdatedAt = now;
    }

    public Application Apply(Guid freelancerId, string proposal, decimal proposedBudget, DateTimeOffset now)
    {
        if (Status != TaskStatus.Published)
            throw new InvalidOperationException("Can only apply to published tasks");

        if (_applications.Any(a => a.FreelancerId == freelancerId && a.Status != ApplicationStatus.Rejected))
            throw new InvalidOperationException("You have already applied to this task");

        var application = Application.Create(Id, freelancerId, proposal, proposedBudget, now);
        _applications.Add(application);

        RaiseDomainEvent(new ApplicationSubmittedDomainEvent(Id, freelancerId, proposedBudget, Currency));

        return application;
    }

    public void AcceptApplication(Guid applicationId, DateTimeOffset now)
    {
        if (Status != TaskStatus.Published)
            throw new InvalidOperationException("Can only accept applications for published tasks");

        var application = _applications.FirstOrDefault(a => a.Id == applicationId)
            ?? throw new InvalidOperationException("Application not found");

        if (application.Status != ApplicationStatus.Pending)
            throw new InvalidOperationException("Application is not pending");

        application.Accept(now);

        // Reject all other pending applications
        foreach (var other in _applications.Where(a => a.Id != applicationId && a.Status == ApplicationStatus.Pending))
            other.Reject(now);

        AssignedFreelancerId = application.FreelancerId;
        Status = TaskStatus.InProgress;
        UpdatedAt = now;

        RaiseDomainEvent(new ApplicationAcceptedDomainEvent(Id, application.FreelancerId, Budget, Currency));
    }

    // Method to accept application by ID when applications are loaded externally
    public void AcceptApplicationById(Guid applicationId, Guid freelancerId, DateTimeOffset now)
    {
        if (Status != TaskStatus.Published)
            throw new InvalidOperationException("Can only accept applications for published tasks");

        AssignedFreelancerId = freelancerId;
        Status = TaskStatus.InProgress;
        UpdatedAt = now;

        RaiseDomainEvent(new ApplicationAcceptedDomainEvent(Id, freelancerId, Budget, Currency));
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status != TaskStatus.InProgress)
            throw new InvalidOperationException("Only in-progress tasks can be completed");

        Status = TaskStatus.Completed;
        CompletedAt = now;
        UpdatedAt = now;

        RaiseDomainEvent(new TaskCompletedDomainEvent(Id, ClientId, AssignedFreelancerId!.Value, Budget, Currency));
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status != TaskStatus.Draft && Status != TaskStatus.Published)
            throw new InvalidOperationException("Can only cancel draft or published tasks");

        Status = TaskStatus.Cancelled;
        UpdatedAt = now;
    }

    public void StartDispute(DateTimeOffset now)
    {
        if (Status != TaskStatus.InProgress && Status != TaskStatus.Completed)
            throw new InvalidOperationException("Can only dispute in-progress or completed tasks");

        Status = TaskStatus.Disputed;
        UpdatedAt = now;
    }
}
