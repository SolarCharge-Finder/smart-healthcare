using Microsoft.EntityFrameworkCore;

namespace AIService.Data;

public class AiDbContext : DbContext
{
    public AiDbContext(DbContextOptions<AiDbContext> options) : base(options)
    {
    }

    // Database sets will be added in next commit
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
