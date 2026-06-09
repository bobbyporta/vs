using Microsoft.EntityFrameworkCore;
using PUBReservationSystem.Models;
using BusRoute = PUBReservationSystem.Models.BusRoute;


namespace PUBReservationSystem.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Employee> Employee { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Bus> Bus { get; set; }
        public DbSet<Trip> Trip { get; set; }
        public DbSet<Reservation> Reservation { get; set; }
        public DbSet<Payment> Payment { get; set; }
        public DbSet<BusRoute> Routes { get; set; }
        public DbSet<AuditLog> AuditLog { get; set; }

        public DbSet<Branch> Branches { get; set; }

        public DbSet<Sale> Sales { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>().ToTable("Employee");
            modelBuilder.Entity<Users>().ToTable("Users");
            modelBuilder.Entity<Bus>().ToTable("Bus");
            modelBuilder.Entity<Trip>().ToTable("Trip");
            modelBuilder.Entity<Reservation>().ToTable("Reservation");
            modelBuilder.Entity<Payment>().ToTable("Payment");
            modelBuilder.Entity<BusRoute>().ToTable("Routes");
            modelBuilder.Entity<AuditLog>().ToTable("Audit_Log");
            // Inside AppDbContext.cs, in the OnModelCreating method:
            modelBuilder.Entity<Branch>().ToTable("Branches");

            // Users → Employee
            modelBuilder.Entity<Users>()
    .HasOne(u => u.Employee)
    .WithMany()
    .HasForeignKey(u => u.Employee_ID)
    .OnDelete(DeleteBehavior.Restrict);

            // Trip → Bus
            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Bus)
                .WithMany(b => b.Trips)
                .HasForeignKey(t => t.Bus_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // Trip → Driver
            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Driver)
                .WithMany()
                .HasForeignKey(t => t.Employee_ID_Driver)
                .OnDelete(DeleteBehavior.Restrict);

            // Trip → Conductor (nullable)
            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Conductor)
                .WithMany()
                .HasForeignKey(t => t.Employee_ID_Conductor)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Reservation → Trip
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Trip)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.Trip_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // Reservation → Users
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.CreatedBy)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.User_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment → Reservation (one-to-one)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Reservation)
                .WithOne(r => r.Payment)
                .HasForeignKey<Payment>(p => p.Reservation_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // AuditLog → Users
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.User_ID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}