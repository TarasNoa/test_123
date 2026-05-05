using Libr4.Tasks.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Tasks.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfig : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.ToTable("projects");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.OwnerId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(5000).IsRequired();
        b.Property(x => x.Category).HasMaxLength(100);
        b.Property(x => x.BudgetMin).HasPrecision(10, 2);
        b.Property(x => x.BudgetMax).HasPrecision(10, 2);
        b.Property(x => x.Budget).HasPrecision(10, 2);
        b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.Client).HasMaxLength(200);
        b.Property(x => x.Status).HasConversion<string>();
        b.Property(x => x.Progress).HasDefaultValue(0);

        b.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Tasks)
            .WithOne()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Milestones)
            .WithOne()
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProjectMemberConfig : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> b)
    {
        b.ToTable("project_members");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ProjectId, x.UserId }).IsUnique();
        b.HasIndex(x => x.UserId);

        b.Property(x => x.Role).HasMaxLength(50).IsRequired();
    }
}

public sealed class ProjectTaskConfig : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> b)
    {
        b.ToTable("project_tasks");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ProjectId);
        b.HasIndex(x => x.AssignedToId);
        b.HasIndex(x => x.Status);

        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.Status).HasConversion<string>();
        b.Property(x => x.Priority).HasConversion<string>();
    }
}

public sealed class MilestoneConfig : IEntityTypeConfiguration<Milestone>
{
    public void Configure(EntityTypeBuilder<Milestone> b)
    {
        b.ToTable("milestones");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ProjectId);
        b.HasIndex(x => x.DueDate);

        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.IsCompleted).HasDefaultValue(false);
    }
}
