using Microsoft.Extensions.Diagnostics.HealthChecks;

using RabbitMQ.Client;

namespace NotificationService.Health;

public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;

    public RabbitMqHealthCheck(IConfiguration config)
    {
        _config = config;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var host = _config["RabbitMQ__Host"] ?? "rabbitmq";

        var factory = new ConnectionFactory
        {
            HostName = host
        };

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
