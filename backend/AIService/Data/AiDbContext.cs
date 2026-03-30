using AIService.Models;
using Microsoft.EntityFrameworkCore;

namespace AIService.Data;

public class AiDbContext : DbContext
{
    public AiDbContext(DbContextOptions<AiDbContext> options) : base(options)
    {
    }

    // Database tables
    public DbSet<AIAnalysis> AIAnalyses => Set<AIAnalysis>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PatientSession> PatientSessions => Set<PatientSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // AI Analysis configuration
        modelBuilder.Entity<AIAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConfidenceScore).HasPrecision(3, 2);
            entity.Property(e => e.CostUsd).HasPrecision(10, 4);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.CorrelationId);
        });

        // Audit Log configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CostUsd).HasPrecision(10, 4);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.RequestIp);
        });

        // Patient Session configuration
        modelBuilder.Entity<PatientSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StartedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.LastActivityAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.SessionToken);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsActive);
        });
    }
}
