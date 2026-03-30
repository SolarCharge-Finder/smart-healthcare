using Microsoft.EntityFrameworkCore;
using PaymentService.Models;

namespace PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.AppointmentId).IsUnique();
            entity.Property(p => p.StripePaymentIntentId).IsRequired();
            entity.Property(p => p.ClientSecret).IsRequired();
            entity.Property(p => p.Currency).HasMaxLength(10);
            entity.Property(p => p.Status).HasMaxLength(30);
            entity.Property(p => p.FailureReason).HasMaxLength(250);
        });
    }
}