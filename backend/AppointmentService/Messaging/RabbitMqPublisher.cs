using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using AppointmentService.Logging;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace AppointmentService.Messaging;

public class RabbitMqPublisher
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(
        IConfiguration config,
        ICorrelationIdAccessor correlationIdAccessor,
        ILogger<RabbitMqPublisher> logger)
    {
        var host =
            Environment.GetEnvironmentVariable("RABBITMQ_HOST") ??
            config["RabbitMQ__Host"] ??
            "rabbitmq";
        var user = config["RabbitMQ__User"];
        var password = config["RabbitMQ__Password"];

        _factory = new ConnectionFactory
        {
            HostName = host
        };

        if (!string.IsNullOrWhiteSpace(user))
            _factory.UserName = user;

        if (!string.IsNullOrWhiteSpace(password))
            _factory.Password = password;

        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
    }

    public async Task PublishAsync(string queueName, object message)
    {
        var json = JsonSerializer.Serialize(message);
        var correlationId = _correlationIdAccessor.CorrelationId;
        await PublishInternalAsync(
            queueName,
            Encoding.UTF8.GetBytes(json),
            correlationId);
    }

    public async Task PublishRawAsync(
        string queueName,
        string json,
        string? correlationId)
    {
        await PublishInternalAsync(
            queueName,
            Encoding.UTF8.GetBytes(json),
            correlationId);
    }

    private async Task PublishInternalAsync(
        string queueName,
        ReadOnlyMemory<byte> body,
        string? correlationId)
    {
        const int maxRetries = 3;
        var dlxExchange = $"{queueName}.dlx";
        var dlqQueue = $"{queueName}.dlq";
        var dlqRoutingKey = dlqQueue;

        using var _ = LogContext.PushProperty(
            "correlationId", correlationId ?? string.Empty);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var connection = await GetOrCreateConnectionAsync();
                await using var channel =
                    await connection.CreateChannelAsync();

                await channel.ExchangeDeclareAsync(
                    exchange: dlxExchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    arguments: null);

                await channel.QueueDeclareAsync(
                    queue: dlqQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                await channel.QueueBindAsync(
                    queue: dlqQueue,
                    exchange: dlxExchange,
                    routingKey: dlqRoutingKey,
                    arguments: null);

                var queueArgs = new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"] = dlxExchange,
                    ["x-dead-letter-routing-key"] = dlqRoutingKey
                };

                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: queueArgs);

                var props = new BasicProperties
                {
                    Persistent = true,
                    CorrelationId = correlationId
                };

                if (!string.IsNullOrWhiteSpace(correlationId))
                {
                    props.Headers = new Dictionary<string, object>
                    {
                        ["x-correlation-id"] = correlationId
                    };
                }

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: queueName,
                    basicProperties: props,
                    body: body);

                _logger.LogInformation(
                    "RabbitMQ publish success {Event}",
                    queueName
                );

                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "RabbitMQ publish failed on attempt {Attempt}",
                    attempt);

                if (attempt == maxRetries)
                {
                    _logger.LogError(
                        ex,
                        "RabbitMQ publish failed after retries {Event}",
                        queueName
                    );
                    throw;
                }

                var delayMs =
                    (int)(100 * Math.Pow(2, attempt - 1));

                await Task.Delay(delayMs);
            }
        }
    }

    private async Task<IConnection> GetOrCreateConnectionAsync()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _connectionLock.WaitAsync();
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            _connection = await _factory.CreateConnectionAsync();
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }
}
