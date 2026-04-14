namespace Doctor.Infrastructure.Data;

using Doctor.Domain.Entities;

using Microsoft.EntityFrameworkCore;


public class DoctorDbContext : DbContext
{
    public DoctorDbContext(DbContextOptions<DoctorDbContext> options)
        : base(options) { }

    public DbSet<Doctor> Doctors => Set<Doctor>();

    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // doctor config - not really necessary but mental illness refactors ig </3
        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.FullName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(d => d.Specialization)
                .HasMaxLength(150);

            entity.Property(d => d.Hospital)
                .HasMaxLength(200);

            entity.Property(d => d.CreatedAt)
                .IsRequired();
        });

        // doctor availability config 
        modelBuilder.Entity<DoctorAvailability>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Hospital)
                .HasMaxLength(200);

            entity.Property(a => a.StartTime)
                .IsRequired();

            entity.Property(a => a.EndTime)
                .IsRequired();

            entity.Property(a => a.IsActive)
                .IsRequired();

            entity.Property(a => a.IsRecurring)
                .IsRequired();

            entity.Property(a => a.CreatedAt)
                .IsRequired();

            // Indexes for performance
            entity.HasIndex(a => a.DoctorId);

            // composite index for common queries
            entity.HasIndex(a => new { a.DoctorId, a.StartTime, a.EndTime });

            // filter active slots for a doctor
            entity.HasIndex(a => new { a.DoctorId, a.IsActive });

        });
    }
}
