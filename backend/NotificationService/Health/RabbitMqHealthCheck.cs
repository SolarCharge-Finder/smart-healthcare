using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using NotificationService.Configuration;

using RabbitMQ.Client;

namespace NotificationService.Health;

public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqOptions _options;

    public RabbitMqHealthCheck(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? _options.Host;

        var factory = new ConnectionFactory
        {
            HostName = host
        };

        if (!string.IsNullOrWhiteSpace(_options.User))
        {
            factory.UserName = _options.User;
        }

        if (!string.IsNullOrWhiteSpace(_options.Password))
        {
            factory.Password = _options.Password;
        }

        try
        {
            await using var connection =
                await factory.CreateConnectionAsync(cancellationToken);

            return HealthCheckResult.Healthy("RabbitMQ reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "RabbitMQ unreachable",
                ex);
        }
    }
}
