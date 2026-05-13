using Libr4.Collaboration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Collaboration.Infrastructure.Persistence.Configurations;

public class CollaborationRoomConfiguration : IEntityTypeConfiguration<CollaborationRoom>
{
    public void Configure(EntityTypeBuilder<CollaborationRoom> builder)
    {
        builder.ToTable("rooms", "collaboration");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Description).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.CreatorId).IsRequired();
        builder.Property(r => r.TaskId);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();
        builder.Property(r => r.Type).IsRequired().HasConversion<string>();
        builder.Property(r => r.IsPublic).IsRequired();
        builder.Property(r => r.Status).IsRequired().HasConversion<string>();

        builder.OwnsMany(r => r.Participants, p =>
        {
            p.ToJson();
            p.WithOwner();
        });

        builder.OwnsMany(r => r.Messages, m =>
        {
            m.ToJson();
            m.WithOwner();
            m.Ignore(x => x.Attachments);
        });

        builder.OwnsMany(r => r.Sessions, s =>
        {
            s.ToJson();
            s.WithOwner();
        });

        builder.OwnsMany(r => r.FileShares, f =>
        {
            f.ToJson();
            f.WithOwner();
        });

        builder.OwnsOne(r => r.Settings, s =>
        {
            s.ToJson();
        });

        builder.HasIndex(r => r.CreatorId);
        builder.HasIndex(r => r.TaskId);
        builder.HasIndex(r => r.Status);
    }
}
