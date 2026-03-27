using AppointmentService.Data;
using AppointmentService.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AppointmentService.Tests;

public class DbModelTests
{
    [Fact]
    public void Appointments_Index_Is_Unique_On_DoctorId_SlotTime()
    {
        using var db = CreateDbContext();
        var entity = db.Model.FindEntityType(typeof(Appointment));

        Assert.NotNull(entity);

        var index = entity!.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 2 &&
                i.Properties.Any(p => p.Name == "DoctorId") &&
                i.Properties.Any(p => p.Name == "SlotTime"));

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public void IdempotencyRecords_Key_Index_Is_Unique()
    {
        using var db = CreateDbContext();
        var entity = db.Model.FindEntityType(
            typeof(IdempotencyRecord));

        Assert.NotNull(entity);

        var index = entity!.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 1 &&
                i.Properties[0].Name == "Key");

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public void OutboxMessages_Has_ProcessedAt_Index()
    {
        using var db = CreateDbContext();
        var entity = db.Model.FindEntityType(
            typeof(OutboxMessage));

        Assert.NotNull(entity);

        var index = entity!.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 1 &&
                i.Properties[0].Name == "ProcessedAt");

        Assert.NotNull(index);
    }

    private static AppointmentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppointmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppointmentDbContext(options);
    }
}
