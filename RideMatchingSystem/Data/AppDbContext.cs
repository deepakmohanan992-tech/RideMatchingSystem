using Microsoft.EntityFrameworkCore;
using RideMatchingSystem.Models;

namespace RideMatchingSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Driver> Drivers { get; set; } = null!;
        public DbSet<Rider> Riders { get; set; } = null!;
        public DbSet<Ride> Rides { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Driver>(b =>
            {
                b.HasKey(d => d.Id);
                b.Property(d => d.RowVersion).IsRowVersion();
                b.Property(d => d.IsAvailable).HasDefaultValue(false);
            });

            modelBuilder.Entity<Rider>(b =>
            {
                b.HasKey(r => r.Id);
            });

            modelBuilder.Entity<Ride>(b =>
            {
                b.HasKey(r => r.Id);
                b.Property(r => r.Status).HasConversion<string>();
            });
        }
    }
}

