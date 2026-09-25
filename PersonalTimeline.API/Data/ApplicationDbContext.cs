using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<TimelineEntry> TimelineEntries { get; set; }
    public DbSet<ApiConnection> ApiConnections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enforce uniqueness on the OAuth fields for safe lookups
        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.OAuthProvider, u.OAuthId })
            .IsUnique();

        // One-to-many relationship from User to TimelineEntry
        modelBuilder.Entity<TimelineEntry>()
            .HasOne(e => e.User)
            .WithMany(u => u.TimelineEntries)
            .HasForeignKey(e => e.UserId);

        // One-to-many relationship from User to ApiConnection
        modelBuilder.Entity<ApiConnection>()
            .HasOne(c => c.User)
            .WithMany(u => u.ApiConnections)
            .HasForeignKey(c => c.UserId);

        // Enforce uniqueness on external API items to prevent duplicate syncs
        modelBuilder.Entity<TimelineEntry>()
            .HasIndex(e => new { e.UserId, e.SourceApi, e.ExternalId })
            .IsUnique();
    }
}
