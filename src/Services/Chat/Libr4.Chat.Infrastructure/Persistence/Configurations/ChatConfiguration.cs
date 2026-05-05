using Libr4.Chat.Domain.Chats;
using ChatEntity = Libr4.Chat.Domain.Chats.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Chat.Infrastructure.Persistence.Configurations;

public class ChatConfiguration : IEntityTypeConfiguration<ChatEntity>
{
    public void Configure(EntityTypeBuilder<ChatEntity> builder)
    {
        builder.ToTable("Chats", "chat");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.RelatedTaskId);

        builder.Property(c => c.CreatedAt);
        builder.Property(c => c.ArchivedAt);

        builder.HasMany(c => c.Members)
            .WithOne()
            .HasForeignKey("ChatId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.Type);
        builder.HasIndex(c => c.RelatedTaskId);
        builder.HasIndex(c => c.CreatedAt);
    }
}
