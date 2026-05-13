using System.Text.Json;
using Libr4.Collaboration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Libr4.Collaboration.Infrastructure.Persistence.Configurations;

public class SharedDocumentConfiguration : IEntityTypeConfiguration<SharedDocument>
{
    public void Configure(EntityTypeBuilder<SharedDocument> builder)
    {
        builder.ToTable("documents", "collaboration");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.RoomId).IsRequired();
        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Type).IsRequired().HasMaxLength(50);
        builder.Property(d => d.OwnerId).IsRequired();
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.Content);

        builder.OwnsMany(d => d.Versions, v =>
        {
            v.ToJson();
            v.WithOwner();
        });

        var guidListConverter = new ValueConverter<List<Guid>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>());

        builder.Property(d => d.CollaboratingUsers)
            .HasConversion(guidListConverter)
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(new ValueComparer<List<Guid>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.OwnsOne(d => d.Permissions, p =>
        {
            p.ToJson();
            p.Ignore(x => x.UserPermissions);
        });

        builder.HasIndex(d => d.RoomId);
    }
}
