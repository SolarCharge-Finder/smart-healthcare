namespace SmartService.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using SmartService.Domain.Entities;

public class SmartServiceDbContext : DbContext
{
    public SmartServiceDbContext(DbContextOptions<SmartServiceDbContext> options)
        : base(options) { }

    public DbSet<Entity> Entities => Set<Entity>();
}