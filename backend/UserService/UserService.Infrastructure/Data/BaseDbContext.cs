namespace UserService.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;

public class UserServiceDbContext : DbContext
{
    public UserServiceDbContext(DbContextOptions<UserServiceDbContext> options)
        : base(options) { }

    public DbSet<Entity> Entities => Set<Entity>();
}