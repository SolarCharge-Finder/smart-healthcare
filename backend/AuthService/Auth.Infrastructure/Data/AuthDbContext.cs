namespace Auth.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Auth.Domain.Entities;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options) { }

    // real users
    public DbSet<User> Users { get; set; }

    // pending users waiting for email verification
    public DbSet<PendingUser> PendingUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // user constraints
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // pending user constraints
        modelBuilder.Entity<PendingUser>()
            .HasIndex(p => p.Email);

        modelBuilder.Entity<PendingUser>()
            .HasIndex(p => p.VerificationToken)
            .IsUnique();
    }
}