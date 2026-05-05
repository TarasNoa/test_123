using Libr4.Tasks.Domain.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Tasks.Infrastructure.Persistence.Configurations;

public sealed class LikeConfig : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> b)
    {
        b.ToTable("likes");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.TargetId, x.TargetType }).IsUnique();
        b.HasIndex(x => x.TargetId);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.TargetType).HasConversion<string>();
    }
}

public sealed class BookmarkConfig : IEntityTypeConfiguration<Bookmark>
{
    public void Configure(EntityTypeBuilder<Bookmark> b)
    {
        b.ToTable("bookmarks");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.TargetId, x.TargetType }).IsUnique();
        b.HasIndex(x => x.TargetId);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.TargetType).HasConversion<string>();
        b.Property(x => x.Notes).HasMaxLength(500);
    }
}

public sealed class FollowConfig : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> b)
    {
        b.ToTable("follows");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.FollowerId, x.FollowingId }).IsUnique();
        b.HasIndex(x => x.FollowingId);
        b.HasIndex(x => x.CreatedAt);
    }
}

public sealed class ViewConfig : IEntityTypeConfiguration<View>
{
    public void Configure(EntityTypeBuilder<View> b)
    {
        b.ToTable("views");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.TargetId, x.TargetType });
        b.HasIndex(x => x.TargetId);
        b.HasIndex(x => x.ViewedAt);

        b.Property(x => x.TargetType).HasConversion<string>();
    }
}
