using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthService.Tests;

public class DbModelTests
{
    [Fact]
    public void Users_Email_Should_Be_Unique()
    {
        using var db = CreateDbContext();
        var entity = db.Model.FindEntityType(typeof(User));

        Assert.NotNull(entity);

        var index = entity!.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Any(p => p.Name == "Email"));

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuthDbContext(options);
    }
}