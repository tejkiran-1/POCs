using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LocationTrackingApi.Models;

namespace LocationTrackingApi.Data
{
    /// <summary>
    /// Database context for the Location Tracking application
    /// Extends IdentityDbContext to include ASP.NET Core Identity functionality
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// DbSet for location records - stores historical GPS data
        /// </summary>
        public DbSet<LocationRecord> LocationRecords { get; set; }

        /// <summary>
        /// Configure entity relationships and database constraints
        /// </summary>
        /// <param name="modelBuilder">Entity Framework model builder</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure LocationRecord entity
            modelBuilder.Entity<LocationRecord>(entity =>
            {
                // Set table name
                entity.ToTable("LocationRecords");

                // Configure primary key
                entity.HasKey(e => e.Id);

                // Configure foreign key relationship with ApplicationUser
                entity.HasOne(e => e.User)
                      .WithMany(u => u.LocationHistory)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Configure indexes for performance
                entity.HasIndex(e => e.UserId)
                      .HasDatabaseName("IX_LocationRecords_UserId");

                entity.HasIndex(e => e.Timestamp)
                      .HasDatabaseName("IX_LocationRecords_Timestamp");

                entity.HasIndex(e => new { e.UserId, e.Timestamp })
                      .HasDatabaseName("IX_LocationRecords_UserId_Timestamp");

                // Configure decimal precision for coordinates
                entity.Property(e => e.Latitude)
                      .HasPrecision(10, 8); // Allows for ~1cm precision

                entity.Property(e => e.Longitude)
                      .HasPrecision(11, 8); // Allows for ~1cm precision

                entity.Property(e => e.Accuracy)
                      .HasPrecision(10, 2);

                entity.Property(e => e.Altitude)
                      .HasPrecision(10, 2);

                entity.Property(e => e.Speed)
                      .HasPrecision(8, 2);

                entity.Property(e => e.Bearing)
                      .HasPrecision(5, 2);
            });

            // Configure ApplicationUser entity extensions
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                // Configure indexes for performance
                entity.HasIndex(e => e.IsOnline)
                      .HasDatabaseName("IX_AspNetUsers_IsOnline");

                entity.HasIndex(e => e.LastSeen)
                      .HasDatabaseName("IX_AspNetUsers_LastSeen");

                // Configure decimal precision for current location
                entity.Property(e => e.CurrentLatitude)
                      .HasPrecision(10, 8);

                entity.Property(e => e.CurrentLongitude)
                      .HasPrecision(11, 8);

                // Configure string lengths
                entity.Property(e => e.DisplayName)
                      .HasMaxLength(100)
                      .IsRequired();
            });
        }

        /// <summary>
        /// Override SaveChanges to automatically set timestamps
        /// </summary>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync to automatically set timestamps
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Automatically update CreatedAt timestamps for new entities
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is ApplicationUser && e.State == EntityState.Added);

            foreach (var entry in entries)
            {
                if (entry.Entity is ApplicationUser user)
                {
                    user.CreatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
