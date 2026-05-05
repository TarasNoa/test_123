using Libr4.Chat.Domain.Chats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Chat.Infrastructure.Persistence.Configurations;

public class ChatMemberConfiguration : IEntityTypeConfiguration<ChatMember>
{
    public void Configure(EntityTypeBuilder<ChatMember> builder)
    {
        builder.ToTable("ChatMembers", "chat");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId);

        builder.Property(m => m.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.JoinedAt);
        builder.Property(m => m.LastReadAt);

        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => new { m.UserId, m.LastReadAt });
    }
}
