using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PaymentService.Data;
using PaymentService.Models;
using PaymentService.Services;
using Stripe;

namespace PaymentService.Tests;

public class PaymentProcessorServiceTests
{
    [Fact]
    public async Task ConfirmPaymentAsync_Updates_Status_To_Succeeded()
    {
        using var db = CreateDbContext();
        var payment = SeedPayment(db, PaymentStatus.Pending);

        var service = CreateService(db);
        var result = await service.ConfirmPaymentAsync(payment.Id, new ConfirmPaymentRequest
        {
            IsSuccess = true
        });

        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Succeeded, result!.Status);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task UpdatePaymentFromStripeIntentAsync_Does_Not_Regress_Terminal_Status()
    {
        using var db = CreateDbContext();
        var payment = SeedPayment(db, PaymentStatus.Succeeded);

        var service = CreateService(db);
        var intent = new PaymentIntent
        {
            Id = payment.StripePaymentIntentId,
            Status = "requires_payment_method",
            ClientSecret = "new_secret"
        };

        var result = await service.UpdatePaymentFromStripeIntentAsync(intent);

        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Succeeded, result!.Status);
        Assert.Equal(payment.ClientSecret, result.ClientSecret);
    }

    private static PaymentProcessorService CreateService(PaymentDbContext db)
    {
        return new PaymentProcessorService(
            db,
            null!,
            new FixedPricingService(),
            NullLogger<PaymentProcessorService>.Instance,
            Options.Create(new StripeOptions
            {
                SecretKey = "sk_test_123"
            }));
    }

    private static PaymentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PaymentDbContext(options);
    }

    private static Payment SeedPayment(PaymentDbContext db, string status)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            AppointmentId = Guid.NewGuid(),
            StripePaymentIntentId = $"pi_{Guid.NewGuid():N}",
            ClientSecret = "old_secret",
            Amount = 50000,
            Currency = "lkr",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Payments.Add(payment);
        db.SaveChanges();
        return payment;
    }

    private sealed class FixedPricingService : IAppointmentPricingService
    {
        public Task<(long Amount, string Currency)> ResolvePricingAsync(
            Guid appointmentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult((50000L, "lkr"));
        }
    }
}
