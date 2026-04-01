namespace AdminService.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using AdminService.Domain.Entities;

public class AdminServiceDbContext : DbContext
{
    public AdminServiceDbContext(DbContextOptions<AdminServiceDbContext> options)
        : base(options) { }

    public DbSet<Entity> Entities => Set<Entity>();
}