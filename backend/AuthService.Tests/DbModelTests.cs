using Auth.Domain.Entities;
using Auth.Infrastructure.Data;

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

    [Fact]
    public void PendingUsers_VerificationToken_Should_Be_Indexed()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(PendingUser));
        Assert.NotNull(entity);

        var index = entity!.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Any(p => p.Name == "VerificationToken"));

        Assert.NotNull(index);
    }

    [Fact]
    public void Users_Email_Should_Be_Required()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(User));
        Assert.NotNull(entity);

        var property = entity!.FindProperty("Email");
        Assert.NotNull(property);

        Assert.False(property!.IsNullable);
    }

    [Fact]
    public void Users_PasswordHash_Should_Be_Required()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(User));
        Assert.NotNull(entity);

        var property = entity!.FindProperty("PasswordHash");
        Assert.NotNull(property);

        Assert.False(property!.IsNullable);
    }

    [Fact]
    public void Users_Should_Have_Role_Property()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(User));
        Assert.NotNull(entity);

        var property = entity!.FindProperty("Role");

        Assert.NotNull(property);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuthDbContext(options);
    }
}
