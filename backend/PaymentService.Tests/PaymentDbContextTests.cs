using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Tests;

public class PaymentDbContextTests
{
    [Fact]
    public void AppointmentId_Index_Is_Unique()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(Payment));

        Assert.NotNull(entity);

        var index = entity!
            .GetIndexes()
            .SingleOrDefault(i =>
                i.Properties.Count == 1 &&
                i.Properties[0].Name == nameof(Payment.AppointmentId));

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public void Payment_String_Fields_Have_Expected_Lengths()
    {
        using var db = CreateDbContext();

        var entity = db.Model.FindEntityType(typeof(Payment));

        Assert.NotNull(entity);

        Assert.Equal(10, entity!.FindProperty(nameof(Payment.Currency))!.GetMaxLength());
        Assert.Equal(30, entity.FindProperty(nameof(Payment.Status))!.GetMaxLength());
        Assert.Equal(250, entity.FindProperty(nameof(Payment.FailureReason))!.GetMaxLength());
    }

    private static PaymentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PaymentDbContext(options);
    }
}
