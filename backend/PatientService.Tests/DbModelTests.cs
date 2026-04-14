using Microsoft.EntityFrameworkCore;

using PatientService.Infrastructure.Data;

using Xunit;

using PatientEntity = PatientService.Domain.Entities.Patient;

namespace PatientService.Tests;

public class DbModelTests
{
    [Fact]
    public void Patient_Should_Have_Required_Fields()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(PatientEntity));

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

        var entity = db.Model.FindEntityType(typeof(PatientEntity));

        var key = entity!.FindPrimaryKey();

        Assert.NotNull(key);
        Assert.Contains(key!.Properties, p => p.Name == "Id");
    }

    private static PatientDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PatientDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PatientDbContext(options);
    }
}
