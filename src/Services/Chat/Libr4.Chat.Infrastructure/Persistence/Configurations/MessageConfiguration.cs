using Libr4.Chat.Domain.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Chat.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages", "chat");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ChatId);
        builder.Property(m => m.SenderId);

        builder.Property(m => m.Content)
            .HasMaxLength(100000);

        builder.Property(m => m.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.Timestamp);
        builder.Ignore(m => m.SentAt);
        builder.Property(m => m.EditedAt);
        builder.Property(m => m.IsDeleted);

        builder.Property(m => m.FileUrl)
            .HasMaxLength(500);

        builder.Property(m => m.FileName)
            .HasMaxLength(255);

        builder.Property(m => m.FileSize);
        builder.Property(m => m.ReplyToMessageId);

        builder.HasIndex(m => m.ChatId);
        builder.HasIndex(m => m.SenderId);
        builder.HasIndex(m => m.Timestamp);
        builder.HasIndex(m => new { m.ChatId, m.Timestamp });
    }
}
