using Doctor.Infrastructure.Data;
using DoctorEntity = Doctor.Domain.Entities.Doctor;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DoctorService.Tests;

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
    public void Doctors_Id_Should_Be_PrimaryKey()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(DoctorEntity));

        var key = entity!.FindPrimaryKey();

        Assert.NotNull(key);
        Assert.Contains(key!.Properties, p => p.Name == "Id");
    }

    private static DoctorDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DoctorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DoctorDbContext(options);
    }
}