using Libr4.Social.Domain.Network;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Social.Infrastructure;

public class SocialDbContext : DbContext
{
    public SocialDbContext(DbContextOptions<SocialDbContext> options) : base(options) { }

    public DbSet<SocialNetwork> SocialNetworks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SocialNetwork>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.OwnsMany(e => e.Connections, nav =>
            {
                nav.HasKey("Id");
                nav.Property(c => c.ConnectedUserId).IsRequired();
                nav.Property(c => c.Type).IsRequired();
                nav.Property(c => c.ConnectedAt).IsRequired();
            });

            entity.OwnsMany(e => e.Posts, nav =>
            {
                nav.HasKey("Id");
                nav.Property(p => p.Content).IsRequired().HasMaxLength(5000);
                nav.Property(p => p.CreatedAt).IsRequired();

                nav.OwnsMany(p => p.Comments, cnav =>
                {
                    cnav.HasKey("Id");
                    cnav.Property(c => c.Text).IsRequired();
                });
            });

            entity.OwnsOne(e => e.Profile, nav =>
            {
                nav.Property(p => p.Name).IsRequired().HasMaxLength(256);
                nav.Property(p => p.Bio).HasMaxLength(1000);
                nav.Property(p => p.ProfileImageUrl);
                nav.Property(p => p.Location);
            });
        });
    }
}