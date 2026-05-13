using Libr4.Collaboration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Collaboration.Infrastructure.Persistence.Configurations;

public class VideoCallConfiguration : IEntityTypeConfiguration<VideoCall>
{
    public void Configure(EntityTypeBuilder<VideoCall> builder)
    {
        builder.ToTable("video_calls", "collaboration");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.RoomId).IsRequired();
        builder.Property(v => v.InitiatorId).IsRequired();
        builder.Property(v => v.Type).IsRequired().HasConversion<string>();
        builder.Property(v => v.Status).IsRequired().HasConversion<string>();
        builder.Property(v => v.StartedAt).IsRequired();
        builder.Property(v => v.EndedAt);
        builder.Property(v => v.IsRecording).IsRequired();

        builder.OwnsMany(v => v.Participants, p =>
        {
            p.ToJson();
            p.WithOwner();
        });

        builder.OwnsMany(v => v.Recordings, r =>
        {
            r.ToJson();
            r.WithOwner();
        });

        builder.HasIndex(v => v.RoomId);
    }
}
