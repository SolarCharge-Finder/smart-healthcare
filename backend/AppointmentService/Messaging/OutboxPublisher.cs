using AppointmentService.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppointmentService.Messaging;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Outbox publish loop failed");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
    }

    private async Task PublishPendingAsync(
        CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<AppointmentDbContext>();
        var publisher = scope.ServiceProvider
            .GetRequiredService<RabbitMqPublisher>();

        await using var tx =
            await db.Database.BeginTransactionAsync(
                stoppingToken);

        var messages = await db.OutboxMessages
            .FromSqlRaw(
                "SELECT * FROM \"OutboxMessages\" " +
                "WHERE \"ProcessedAt\" IS NULL " +
                "ORDER BY \"CreatedAt\" " +
                "FOR UPDATE SKIP LOCKED " +
                "LIMIT 50")
            .ToListAsync(stoppingToken);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishRawAsync(
                    message.EventName,
                    message.Payload,
                    message.CorrelationId);

                message.ProcessedAt = DateTime.UtcNow;
                Metrics.IncRabbitMqPublishSuccess();
            }
            catch (Exception)
            {
                Metrics.IncRabbitMqPublishFailure();
            }
        }

        await db.SaveChangesAsync(stoppingToken);
        await tx.CommitAsync(stoppingToken);
    }
}
