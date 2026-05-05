using Libr4.Tasks.Domain.CRM;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Tasks.Infrastructure.Persistence.Configurations;

public sealed class CRMAccountConfig : IEntityTypeConfiguration<CRMAccount>
{
    public void Configure(EntityTypeBuilder<CRMAccount> b)
    {
        b.ToTable("crm_accounts");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.OwnerId);
        b.HasIndex(x => x.CompanyName);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Industry).HasMaxLength(100);
        b.Property(x => x.CompanySize).HasMaxLength(50);
        b.Property(x => x.Website).HasMaxLength(500);
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Address).HasMaxLength(1000);
        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.SubscriptionPlan).HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.AiConfiguration).HasColumnType("jsonb");
        b.Property(x => x.AutomationSettings).HasColumnType("jsonb");

        b.HasMany(x => x.Contacts)
            .WithOne()
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Deals)
            .WithOne()
            .HasForeignKey(d => d.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Tasks)
            .WithOne()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Activities)
            .WithOne()
            .HasForeignKey(a => a.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Pipelines)
            .WithOne()
            .HasForeignKey(p => p.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CRMContactConfig : IEntityTypeConfiguration<CRMContact>
{
    public void Configure(EntityTypeBuilder<CRMContact> b)
    {
        b.ToTable("crm_contacts");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.AccountId);
        b.HasIndex(x => x.Email);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.FirstName).HasMaxLength(100);
        b.Property(x => x.LastName).HasMaxLength(100);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.Mobile).HasMaxLength(50);
        b.Property(x => x.Company).HasMaxLength(200);
        b.Property(x => x.JobTitle).HasMaxLength(100);
        b.Property(x => x.Department).HasMaxLength(100);
        b.Property(x => x.LinkedIn).HasMaxLength(500);
        b.Property(x => x.Street).HasMaxLength(200);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.State).HasMaxLength(100);
        b.Property(x => x.Country).HasMaxLength(100);
        b.Property(x => x.PostalCode).HasMaxLength(20);
        b.Property(x => x.Notes).HasMaxLength(5000);
        b.Property(x => x.LeadSource).HasMaxLength(100);
        b.Property(x => x.LeadStatus).HasMaxLength(50);
        b.Property(x => x.PreferredContactMethod).HasMaxLength(50);
        b.Property(x => x.Tags).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.CustomFields).HasColumnType("jsonb");
    }
}

public sealed class CRMDealConfig : IEntityTypeConfiguration<CRMDeal>
{
    public void Configure(EntityTypeBuilder<CRMDeal> b)
    {
        b.ToTable("crm_deals");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.AccountId);
        b.HasIndex(x => x.ContactId);
        b.HasIndex(x => x.Stage);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.DealName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        b.Property(x => x.Stage).HasConversion<string>();
        b.Property(x => x.Competitors).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.NextSteps).HasMaxLength(5000);
        b.Property(x => x.LossReason).HasMaxLength(5000);
        b.Property(x => x.CustomFields).HasColumnType("jsonb");
    }
}

public sealed class CRMTaskConfig : IEntityTypeConfiguration<CRMTask>
{
    public void Configure(EntityTypeBuilder<CRMTask> b)
    {
        b.ToTable("crm_tasks");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.AccountId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.Priority);
        b.HasIndex(x => x.DueDate);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.Status).HasConversion<string>();
        b.Property(x => x.Priority).HasConversion<string>();
        b.Property(x => x.TaskType).HasMaxLength(50).IsRequired();
    }
}

public sealed class CRMActivityConfig : IEntityTypeConfiguration<CRMActivity>
{
    public void Configure(EntityTypeBuilder<CRMActivity> b)
    {
        b.ToTable("crm_activities");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.AccountId);
        b.HasIndex(x => x.ContactId);
        b.HasIndex(x => x.DealId);
        b.HasIndex(x => x.ActivityType);
        b.HasIndex(x => x.ActivityDate);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.Subject).HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.ActivityType).HasConversion<string>();
    }
}

public sealed class CRMPipelineConfig : IEntityTypeConfiguration<CRMPipeline>
{
    public void Configure(EntityTypeBuilder<CRMPipeline> b)
    {
        b.ToTable("crm_pipelines");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.AccountId);
        b.HasIndex(x => x.IsDefault);
        b.HasIndex(x => x.IsActive);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.Stages).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
    }
}
