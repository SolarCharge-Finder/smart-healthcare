using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NotificationService.Services;

public class ReminderScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderScheduler> _logger;

    public ReminderScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<ReminderScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IAppointmentNotificationProcessor>();

                var created = await processor.GenerateRemindersAsync(stoppingToken);
                if (created > 0)
                {
                    _logger.LogInformation("Generated {Count} reminder notifications.", created);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reminder scheduler loop failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
