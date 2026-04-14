using Microsoft.EntityFrameworkCore;

using NotificationService.Models;

namespace NotificationService.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    public DbSet<ProcessedEventEntity> ProcessedEvents => Set<ProcessedEventEntity>();
    public DbSet<AppointmentProjectionEntity> AppointmentProjections => Set<AppointmentProjectionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationEntity>(entity =>
        {
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.IsRead);
            entity.HasIndex(x => new { x.UserId, x.IsRead });
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.ReadAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<ProcessedEventEntity>(entity =>
        {
            entity.HasIndex(x => x.EventKey).IsUnique();
            entity.Property(x => x.ProcessedAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<AppointmentProjectionEntity>(entity =>
        {
            entity.HasIndex(x => x.AppointmentId).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.SlotTimeUtc });
            entity.HasIndex(x => x.ReminderSentAt);
            entity.Property(x => x.SlotTimeUtc).HasColumnType("timestamp with time zone");
            entity.Property(x => x.ReminderSentAt).HasColumnType("timestamp with time zone");
            entity.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        });
    }
}
