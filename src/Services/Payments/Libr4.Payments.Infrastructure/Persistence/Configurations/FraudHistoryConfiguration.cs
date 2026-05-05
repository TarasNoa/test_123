using Libr4.Payments.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Payments.Infrastructure.Persistence.Configurations;

public class FraudHistoryConfiguration : IEntityTypeConfiguration<FraudHistory>
{
    public void Configure(EntityTypeBuilder<FraudHistory> builder)
    {
        builder.ToTable("fraud_history");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedNever();
        
        builder.Property(x => x.UserId)
            .IsRequired();
        
        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(x => x.RecordedAt)
            .IsRequired();
        
        builder.Property(x => x.InvoiceId)
            .HasMaxLength(100);
        
        // Index for efficient lookups by user
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("idx_fraud_history_user_id");
        
        // Composite index for time-based queries
        builder.HasIndex(x => new { x.UserId, x.RecordedAt })
            .HasDatabaseName("idx_fraud_history_user_time");
    }
}
