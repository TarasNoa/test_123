using Libr4.Chat.Domain.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Chat.Infrastructure.Persistence.Configurations;

public class ServerConfiguration : IEntityTypeConfiguration<Server>
{
    public void Configure(EntityTypeBuilder<Server> builder)
    {
        builder.ToTable("servers", "chat");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.OwnerId).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();

        // Owned collections
        builder.OwnsMany(s => s.Channels, cb =>
        {
            cb.ToTable("server_channels", "chat");
            cb.WithOwner().HasForeignKey("ServerId");
            cb.HasKey("Id");
            cb.Property(c => c.Name).IsRequired().HasMaxLength(100);
        });

        builder.OwnsMany(s => s.Members, mb =>
        {
            mb.ToTable("server_members", "chat");
            mb.WithOwner().HasForeignKey("ServerId");
            mb.HasKey("Id");
            mb.Property(m => m.UserId).IsRequired();
        });

        builder.OwnsMany(s => s.Roles, rb =>
        {
            rb.ToTable("server_roles", "chat");
            rb.WithOwner().HasForeignKey("ServerId");
            rb.HasKey("Id");
            rb.Property(r => r.Name).IsRequired().HasMaxLength(100);
        });

        builder.OwnsMany(s => s.ScheduledCalls, scb =>
        {
            scb.ToTable("server_scheduled_calls", "chat");
            scb.WithOwner().HasForeignKey("ServerId");
            scb.HasKey("Id");
            scb.Property(c => c.Title).IsRequired().HasMaxLength(200);
        });

        builder.OwnsMany(s => s.Tasks, tb =>
        {
            tb.ToTable("server_tasks", "chat");
            tb.WithOwner().HasForeignKey("ServerId");
            tb.HasKey("Id");
            tb.Property(t => t.Title).IsRequired().HasMaxLength(200);
        });

        builder.OwnsOne(s => s.Settings, sb =>
        {
            sb.ToTable("server_settings", "chat");
            sb.WithOwner().HasForeignKey("ServerId");
            sb.Property(x => x.WelcomeMessage).HasMaxLength(1000);
        });
    }
}
