using AppointmentService.Models;

using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Data;

public class AppointmentDbContext : DbContext
{
    public AppointmentDbContext(
        DbContextOptions<AppointmentDbContext> options
    ) : base(options)
    {
    }

    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();
    public DbSet<GuestUser> GuestUsers => Set<GuestUser>();
    public DbSet<IdempotencyRecord> IdempotencyRecords =>
        Set<IdempotencyRecord>();
    public DbSet<OutboxMessage> OutboxMessages =>
        Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>()
            .HasIndex(a => new { a.DoctorId, a.SlotTime })
            .IsUnique();

        modelBuilder.Entity<Appointment>()
            .HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.AppointmentNumber })
            .IsUnique();

        modelBuilder.Entity<Appointment>()
            .HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.UserId })
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL AND \"Status\" <> 'CANCELLED' AND \"Status\" <> 'DECLINED'");

        modelBuilder.Entity<Appointment>()
            .Property(a => a.DoctorFee)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.HospitalFee)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.EChannellingFee)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.Discount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.TotalFee)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.GuestUser)
            .WithMany(g => g.Appointments)
            .HasForeignKey(a => a.GuestUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DoctorAvailability>()
            .HasIndex(a => new { a.DoctorId, a.SlotTime })
            .IsUnique();

        modelBuilder.Entity<DoctorAvailability>()
            .Property(a => a.DoctorFee)
            .HasPrecision(10, 2);

        modelBuilder.Entity<DoctorAvailability>()
            .Property(a => a.HospitalFee)
            .HasPrecision(10, 2);

        modelBuilder.Entity<DoctorAvailability>()
            .Property(a => a.EChannellingFee)
            .HasPrecision(10, 2);

        modelBuilder.Entity<DoctorAvailability>()
            .Property(a => a.Discount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<IdempotencyRecord>()
            .HasIndex(r => r.Key)
            .IsUnique();

        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(m => m.ProcessedAt);

        base.OnModelCreating(modelBuilder);
    }
}
