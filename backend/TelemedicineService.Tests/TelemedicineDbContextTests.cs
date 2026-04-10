using Microsoft.EntityFrameworkCore;

using TelemedicineService.Data;
using TelemedicineService.Models;

namespace TelemedicineService.Tests;

public class TelemedicineDbContextTests
{
    [Fact]
    public void AppointmentId_Index_Is_Unique()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(TelemedicineSession));

        Assert.NotNull(entity);

        var index = entity!
            .GetIndexes()
            .SingleOrDefault(i =>
                i.Properties.Count == 1 &&
                i.Properties[0].Name == nameof(TelemedicineSession.AppointmentId));

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public void TokenExpiresAt_Index_Exists()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(TelemedicineSession));

        Assert.NotNull(entity);

        var index = entity!
            .GetIndexes()
            .SingleOrDefault(i =>
                i.Properties.Count == 1 &&
                i.Properties[0].Name == nameof(TelemedicineSession.TokenExpiresAt));

        Assert.NotNull(index);
    }

    private static TelemedicineDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TelemedicineDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TelemedicineDbContext(options);
    }
}
