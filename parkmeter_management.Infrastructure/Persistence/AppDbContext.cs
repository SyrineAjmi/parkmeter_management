using Microsoft.EntityFrameworkCore;
using parkmeter_management.Domain.Entities;

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
        public DbSet<MachineEvent> MachineEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Alert
            modelBuilder.Entity<Alert>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.Priority).HasConversion<string>();
                entity.HasOne(e => e.Parkmeter)
                      .WithMany()
                      .HasForeignKey(e => e.ParkmeterId)
                      .OnDelete(DeleteBehavior.Restrict);
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
                entity.Property(e => e.StrategyType).HasConversion<string>();
                entity.Property(e => e.PricePerHour).HasColumnType("decimal(18,3)");
                entity.Property(e => e.PeakRate).HasColumnType("decimal(18,3)");
                entity.Property(e => e.OffPeakRate).HasColumnType("decimal(18,3)");
                entity.Property(e => e.WeekdayRate).HasColumnType("decimal(18,3)");
                entity.Property(e => e.WeekendRate).HasColumnType("decimal(18,3)");
            });

            // Transaction
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,3)");
                entity.Property(e => e.Status).HasConversion<string>();
                entity.HasOne(e => e.Parkmeter)  
                      .WithMany()
                      .HasForeignKey(e => e.ParkmeterId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Session (with Status)
            modelBuilder.Entity<Session>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,3)");
                entity.Property(e => e.Status).HasConversion<string>();
                entity.HasOne(e => e.Parkmeter)
                      .WithMany()
                      .HasForeignKey(e => e.ParkmeterId)
                      .OnDelete(DeleteBehavior.Restrict);
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

            //event 
            modelBuilder.Entity<MachineEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).HasConversion<int>();
                entity.HasOne(e => e.Parkmeter).WithMany()
                      .HasForeignKey(e => e.ParkmeterId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.ParkmeterId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // interv order 
            modelBuilder.Entity<InterventionOrder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.Status).HasConversion<string>();
                entity.HasOne(e => e.Operator)
                      .WithMany()
                      .HasForeignKey(e => e.OperatorId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Parkmeter)
                      .WithMany()
                      .HasForeignKey(e => e.ParkmeterId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Parkmeter>()
            .Property(p => p.CashAmount)
            .HasColumnType("decimal(18,3)");

            base.OnModelCreating(modelBuilder);
            
        }
    }
}