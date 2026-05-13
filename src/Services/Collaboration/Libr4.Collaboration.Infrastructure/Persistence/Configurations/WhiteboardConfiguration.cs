using Libr4.Collaboration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Collaboration.Infrastructure.Persistence.Configurations;

public class WhiteboardConfiguration : IEntityTypeConfiguration<Whiteboard>
{
    public void Configure(EntityTypeBuilder<Whiteboard> builder)
    {
        builder.ToTable("whiteboards", "collaboration");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.RoomId).IsRequired();
        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.CreatedAt).IsRequired();

        builder.OwnsMany(w => w.Elements, e =>
        {
            e.ToJson();
            e.WithOwner();
        });

        builder.OwnsOne(w => w.CurrentToolState, t =>
        {
            t.ToJson();
        });

        builder.HasIndex(w => w.RoomId);
    }
}
