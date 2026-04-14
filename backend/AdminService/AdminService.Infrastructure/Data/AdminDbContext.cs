namespace AdminService.Infrastructure.Data;

using AdminService.Domain.Entities;

using Microsoft.EntityFrameworkCore;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options)
        : base(options)
    {
    }

    public DbSet<Admin> Admins { get; set; } = null!;
}
