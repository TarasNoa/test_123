using Libr4.Chat.Domain.Calls;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Chat.Infrastructure.Persistence.Configurations;

public class CallConfiguration : IEntityTypeConfiguration<Call>
{
    public void Configure(EntityTypeBuilder<Call> builder)
    {
        builder.ToTable("Calls", "chat");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ChatId).IsRequired();
        builder.Property(c => c.InitiatorId).IsRequired();
        builder.Property(c => c.Type).IsRequired();
        builder.Property(c => c.Status).IsRequired();
        builder.Property(c => c.StartedAt).IsRequired();
        builder.Property(c => c.EndedAt);

        builder.OwnsMany(c => c.Participants, p =>
        {
            p.ToJson();
            p.WithOwner();
        });
    }
}
