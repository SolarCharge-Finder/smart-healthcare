namespace AIService.Services;

public interface IInfrastructureStatusService
{
    bool RedisConnected { get; }
    bool RabbitMqConnected { get; }
    DateTime LastCheckedUtc { get; }
    void SetRedisStatus(bool connected);
    void SetRabbitMqStatus(bool connected);
    object GetSummary();
}

public class InfrastructureStatusService : IInfrastructureStatusService
{
    private readonly object _lock = new();

    public bool RedisConnected { get; private set; }
    public bool RabbitMqConnected { get; private set; }
    public DateTime LastCheckedUtc { get; private set; } = DateTime.UtcNow;

    public void SetRedisStatus(bool connected)
    {
        lock (_lock)
        {
            RedisConnected = connected;
            LastCheckedUtc = DateTime.UtcNow;
        }
    }

    public void SetRabbitMqStatus(bool connected)
    {
        lock (_lock)
        {
            RabbitMqConnected = connected;
            LastCheckedUtc = DateTime.UtcNow;
        }
    }

    public object GetSummary()
    {
        lock (_lock)
        {
            var allHealthy = RedisConnected && RabbitMqConnected;
            return new
            {
                status = allHealthy ? "healthy" : "degraded",
                dependencies = new
                {
                    redis = RedisConnected ? "up" : "down",
                    rabbitmq = RabbitMqConnected ? "up" : "down"
                },
                lastCheckedUtc = LastCheckedUtc
            };
        }
    }
}
