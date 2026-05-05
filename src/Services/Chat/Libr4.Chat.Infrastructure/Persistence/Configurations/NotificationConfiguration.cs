using Libr4.Chat.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Chat.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", "chat");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId);

        builder.Property(n => n.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(n => n.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(n => n.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(n => n.IsRead);
        builder.Property(n => n.CreatedAt);
        builder.Property(n => n.ReadAt);

        builder.Property(n => n.ActionUrl)
            .HasMaxLength(500);

        builder.Property(n => n.RelatedEntityId)
            .HasMaxLength(50);

        builder.Property(n => n.RelatedEntityType)
            .HasMaxLength(50);

        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => new { n.UserId, n.IsRead });
        builder.HasIndex(n => n.CreatedAt);
    }
}
