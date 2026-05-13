using Libr4.Collaboration.Domain;
using Libr4.Collaboration.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Collaboration.Infrastructure.Persistence;

public class CollaborationDbContext : DbContext
{
    public CollaborationDbContext(DbContextOptions<CollaborationDbContext> options) : base(options) { }

    public DbSet<CollaborationRoom> Rooms => Set<CollaborationRoom>();
    public DbSet<SharedDocument> Documents => Set<SharedDocument>();
    public DbSet<Whiteboard> Whiteboards => Set<Whiteboard>();
    public DbSet<VideoCall> VideoCalls => Set<VideoCall>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("collaboration");
        modelBuilder.ApplyConfiguration(new CollaborationRoomConfiguration());
        modelBuilder.ApplyConfiguration(new SharedDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new WhiteboardConfiguration());
        modelBuilder.ApplyConfiguration(new VideoCallConfiguration());
    }
}
