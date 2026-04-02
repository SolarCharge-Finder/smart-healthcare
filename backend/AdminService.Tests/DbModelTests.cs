using AdminService.Infrastructure.Data;
using DoctorEntity = AdminService.Domain.Entities.Doctor;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AdminService.Tests;

public class DbModelTests
{
    [Fact]
    public void Doctors_Should_Have_Required_Fields()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(DoctorEntity));

        Assert.NotNull(entity);

        var properties = entity!.GetProperties();

        Assert.Contains(properties, p => p.Name == "FullName");
        Assert.Contains(properties, p => p.Name == "Specialization");
        Assert.Contains(properties, p => p.Name == "Hospital");
    }

    [Fact]
    public void Admin_Id_Should_Be_PrimaryKey()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(AdminEntity));

        var key = entity!.FindPrimaryKey();

        Assert.NotNull(key);
        Assert.Contains(key!.Properties, p => p.Name == "Id");
    }

    private static AdminDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AdminDbContext(options);
    }
}