using Microsoft.EntityFrameworkCore;
using parkmeter_management.Domain.Entities;
using System.Collections.Specialized;

namespace parkmeter_management.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Alert> Alerts { get; set; }
        public DbSet<Parkmeter> Parkmeters { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<ParkMeterConfig> Configurations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<InterventionOrder> InterventionOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Alert
            modelBuilder.Entity<Alert>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.Priority).HasConversion<string>();
            });

            // Parkmeter
            modelBuilder.Entity<Parkmeter>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SerialNumber);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.HasOne(e => e.Site)
                      .WithMany()
                      .HasForeignKey(e => e.SiteId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuration
            modelBuilder.Entity<ParkMeterConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PricePerHour).HasColumnType("decimal(18,2)");
            });

            // Transaction
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasConversion<string>();
            });

            // Session (with Status)
            modelBuilder.Entity<Session>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasConversion<string>();
            });

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Role).HasConversion<string>();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Site
            modelBuilder.Entity<Site>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            base.OnModelCreating(modelBuilder);
            
        }
    }
}