using Libr4.Tasks.Domain.Reviews;
using Libr4.Tasks.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Tasks.Infrastructure.Persistence.Configurations;

public sealed class TaskAggregateConfiguration : IEntityTypeConfiguration<TaskAggregate>
{
    public void Configure(EntityTypeBuilder<TaskAggregate> e)
    {
        e.ToTable("tasks");
        e.HasKey(x => x.Id);
        e.Property(x => x.Title).IsRequired().HasMaxLength(200);
        e.Property(x => x.Description).IsRequired().HasMaxLength(5000);
        e.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        e.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        e.Property(x => x.Budget).HasPrecision(18, 2);

        e.HasIndex(x => x.Status);
        e.HasIndex(x => x.Category);
        e.HasIndex(x => x.ClientId);
        e.HasIndex(x => x.AssignedFreelancerId);
        e.HasIndex(x => x.CreatedAt);

        // Applications as separate entity with HasMany relationship
        e.HasMany(x => x.Applications)
            .WithOne()
            .HasForeignKey("TaskId")
            .OnDelete(DeleteBehavior.Cascade);

        e.Ignore(x => x.DomainEvents);
    }
}

public sealed class ApplicationConfiguration : IEntityTypeConfiguration<Libr4.Tasks.Domain.Tasks.Application>
{
    public void Configure(EntityTypeBuilder<Libr4.Tasks.Domain.Tasks.Application> e)
    {
        e.ToTable("applications");
        e.HasKey(x => x.Id);
        e.Property(x => x.Proposal).IsRequired().HasMaxLength(2000);
        e.Property(x => x.ProposedBudget).HasPrecision(18, 2);
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        e.Property(x => x.TaskId).IsRequired();
        e.Property(x => x.FreelancerId).IsRequired();
        e.Property(x => x.SubmittedAt).IsRequired();
        e.Property(x => x.RespondedAt);

        e.HasIndex(x => x.TaskId);
        e.HasIndex(x => x.FreelancerId);
        e.HasIndex(x => x.Status);
    }
}

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> e)
    {
        e.ToTable("reviews");
        e.HasKey(x => x.Id);
        e.Property(x => x.Comment).IsRequired().HasMaxLength(1000);
        e.Property(x => x.Rating);

        e.HasIndex(x => x.TaskId);
        e.HasIndex(x => x.ReviewerId);
        e.HasIndex(x => x.RevieweeId);
        e.HasIndex(x => x.CreatedAt);

        e.Ignore(x => x.DomainEvents);
    }
}
