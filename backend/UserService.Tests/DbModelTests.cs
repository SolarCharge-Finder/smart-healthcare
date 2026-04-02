using UserService.Infrastructure.Data;
using UserEntity = UserService.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace UserService.Tests;

public class DbModelTests
{
    [Fact]
    public void User_Should_Have_Required_Fields()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(UserEntity));

        Assert.NotNull(entity);

        var properties = entity!.GetProperties();

        Assert.Contains(properties, p => p.Name == "FullName");
        Assert.Contains(properties, p => p.Name == "Email");
        Assert.Contains(properties, p => p.Name == "UserId");
        Assert.Contains(properties, p => p.Name == "IsActive");
    }

    [Fact]
    public void User_Id_Should_Be_PrimaryKey()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(UserEntity));

        var key = entity!.FindPrimaryKey();

        Assert.NotNull(key);
        Assert.Contains(key!.Properties, p => p.Name == "Id");
    }

    private static UserDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new UserDbContext(options);
    }
}