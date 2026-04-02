namespace AdminService.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using AdminService.Domain.Entities;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options)
        : base(options)
    {
    }

    public DbSet<Admin> Admins { get; set; } = null!;
}