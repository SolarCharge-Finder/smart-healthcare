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
    public DbSet<IdempotencyRecord> IdempotencyRecords =>
        Set<IdempotencyRecord>();
    public DbSet<OutboxMessage> OutboxMessages =>
        Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>()
            .HasIndex(a => new { a.DoctorId, a.SlotTime })
            .IsUnique();

        modelBuilder.Entity<IdempotencyRecord>()
            .HasIndex(r => r.Key)
            .IsUnique();

        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(m => m.ProcessedAt);

        base.OnModelCreating(modelBuilder);
    }
}
