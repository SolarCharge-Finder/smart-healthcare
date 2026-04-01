using Microsoft.EntityFrameworkCore;
using TelemedicineService.Models;

namespace TelemedicineService.Data;

/// <summary>
/// Entity Framework Core DbContext for telemedicine sessions.
/// Manages persistence of video session metadata and token expiry information.
/// </summary>
public class TelemedicineDbContext : DbContext
{
    public TelemedicineDbContext(
        DbContextOptions<TelemedicineDbContext> options
    ) : base(options)
    {
    }

    /// <summary>
    /// Telemedicine sessions table.
    /// </summary>
    public DbSet<TelemedicineSession> TelemedicineSessions =>
        Set<TelemedicineSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ensure each appointment has at most one active session
        modelBuilder.Entity<TelemedicineSession>()
            .HasIndex(s => s.AppointmentId)
            .IsUnique();

        // Index for cleanup queries (finding expired sessions)
        modelBuilder.Entity<TelemedicineSession>()
            .HasIndex(s => s.TokenExpiresAt);
    }
}
