using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace NotificationService.Services;

public class RabbitMqConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMqConsumer> _logger;

    public RabbitMqConsumer(
        IConfiguration config,
        ILogger<RabbitMqConsumer> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host =
            Environment.GetEnvironmentVariable("RABBITMQ_HOST") ??
            _config["RabbitMQ__Host"] ??
            "rabbitmq";
        var user = _config["RabbitMQ__User"];
        var password = _config["RabbitMQ__Password"];

        var factory = new ConnectionFactory
        {
            HostName = host
        };

        if (!string.IsNullOrWhiteSpace(user))
            factory.UserName = user;

        if (!string.IsNullOrWhiteSpace(password))
            factory.Password = password;

        IConnection? connection = null;

        // retry loop until RabbitMQ ready
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                connection = await factory.CreateConnectionAsync();
                using (LogContext.PushProperty(
                    "correlationId", string.Empty))
                {
                    _logger.LogInformation(
                        "Connected to RabbitMQ");
                }
                break;
            }
            catch
            {
                using (LogContext.PushProperty(
                    "correlationId", string.Empty))
                {
                    _logger.LogWarning(
                        "RabbitMQ not ready, retrying in 5 seconds...");
                }
                await Task.Delay(5000, stoppingToken);
            }
        }

        var channel = await connection!.CreateChannelAsync();

        var queueName = "appointment.created";
        var dlxExchange = $"{queueName}.dlx";
        var dlqQueue = $"{queueName}.dlq";
        var dlqRoutingKey = dlqQueue;

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
            arguments: null
        );

        await channel.QueueBindAsync(
            queue: dlqQueue,
            exchange: dlxExchange,
            routingKey: dlqRoutingKey,
            arguments: null
        );

        var queueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = dlxExchange,
            ["x-dead-letter-routing-key"] = dlqRoutingKey
        };

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs
        );

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var correlationId = GetCorrelationId(eventArgs);
            using var _ = LogContext.PushProperty(
                "correlationId", correlationId);

            try
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation(
                    "Message consumed {Queue} {Message}",
                    queueName,
                    message);

                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Message processing failed. " +
                    "Nacking to DLQ {DlqQueue}",
                    dlqQueue);

                await channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer
        );

        var dlqConsumer = new AsyncEventingBasicConsumer(channel);

        dlqConsumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            var correlationId = GetCorrelationId(eventArgs);
            using var _ = LogContext.PushProperty(
                "correlationId", correlationId);

            _logger.LogWarning(
                "Message moved to dead letter queue {Queue} {Message}",
                dlqQueue,
                message);

            await channel.BasicAckAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false);
        };

        await channel.BasicConsumeAsync(
            queue: dlqQueue,
            autoAck: false,
            consumer: dlqConsumer
        );

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static string GetCorrelationId(
        BasicDeliverEventArgs eventArgs)
    {
        var props = eventArgs.BasicProperties;

        if (!string.IsNullOrWhiteSpace(props?.CorrelationId))
            return props!.CorrelationId;

        if (props?.Headers is null)
            return string.Empty;

        if (!props.Headers.TryGetValue(
            "x-correlation-id",
            out var headerValue))
            return string.Empty;

        return headerValue switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string value => value,
            _ => string.Empty
        };
    }
}
