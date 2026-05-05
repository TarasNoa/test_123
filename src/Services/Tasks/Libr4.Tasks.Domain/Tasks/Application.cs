using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.Tasks;

public enum ApplicationStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Withdrawn = 3
}

public sealed class Application : Entity<Guid>
{
    public Guid TaskId { get; private set; }
    public Guid FreelancerId { get; private set; }
    public string Proposal { get; private set; } = null!;
    public decimal ProposedBudget { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }

    private Application() { }

    internal static Application Create(Guid taskId, Guid freelancerId, string proposal, decimal proposedBudget, DateTimeOffset now)
    {
        return new Application
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            FreelancerId = freelancerId,
            Proposal = proposal.Trim(),
            ProposedBudget = proposedBudget,
            Status = ApplicationStatus.Pending,
            SubmittedAt = now
        };
    }

    public void Accept(DateTimeOffset now)
    {
        Status = ApplicationStatus.Accepted;
        RespondedAt = now;
    }

    public void Reject(DateTimeOffset now)
    {
        Status = ApplicationStatus.Rejected;
        RespondedAt = now;
    }

    public void Withdraw(DateTimeOffset now)
    {
        if (Status != ApplicationStatus.Pending)
            throw new InvalidOperationException("Can only withdraw pending applications");

        Status = ApplicationStatus.Withdrawn;
        RespondedAt = now;
    }
}
