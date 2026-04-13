using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NotificationService.Configuration;
using NotificationService.Events;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Services;

public class RabbitMqConsumer : BackgroundService
{
    private static readonly string[] Queues =
    [
        AppointmentEventNames.Created,
        AppointmentEventNames.Confirmed,
        AppointmentEventNames.PaymentConfirmed,
        AppointmentEventNames.Cancelled,
        AppointmentEventNames.Declined
    ];

    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumer> _logger;

    public RabbitMqConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName =
                Environment.GetEnvironmentVariable("RABBITMQ_HOST") ??
                _options.Host
        };

        if (!string.IsNullOrWhiteSpace(_options.User))
        {
            factory.UserName = _options.User;
        }

        if (!string.IsNullOrWhiteSpace(_options.Password))
        {
            factory.Password = _options.Password;
        }

        IConnection? connection = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                connection = await factory.CreateConnectionAsync();
                _logger.LogInformation("Connected to RabbitMQ.");
                break;
            }
            catch
            {
                _logger.LogWarning("RabbitMQ not ready, retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }

        var channel = await connection!.CreateChannelAsync();

        foreach (var queueName in Queues)
        {
            await SetupQueueWithDlqAsync(channel, queueName);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, eventArgs) =>
            {
                try
                {
                    var body = eventArgs.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    var payload = JsonSerializer.Deserialize<AppointmentEventPayload>(
                        message,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (payload is null)
                    {
                        throw new InvalidOperationException("Event payload is empty.");
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<IAppointmentNotificationProcessor>();

                    await processor.ProcessAsync(queueName, payload, stoppingToken);

                    await channel.BasicAckAsync(
                        deliveryTag: eventArgs.DeliveryTag,
                        multiple: false);
                }
                catch (Exception ex)
                {
                    NotificationMetrics.IncEventFailures();

                    _logger.LogError(
                        ex,
                        "Failed to process notification event from queue {Queue}",
                        queueName);

                    await channel.BasicNackAsync(
                        deliveryTag: eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            var dlqQueue = $"{queueName}.dlq";
            var dlqConsumer = new AsyncEventingBasicConsumer(channel);
            dlqConsumer.ReceivedAsync += async (_, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogWarning(
                    "Notification event moved to DLQ {Queue}: {Message}",
                    dlqQueue,
                    message);

                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false);
            };

            await channel.BasicConsumeAsync(
                queue: dlqQueue,
                autoAck: false,
                consumer: dlqConsumer);
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static async Task SetupQueueWithDlqAsync(IChannel channel, string queueName)
    {
        var dlxExchange = $"{queueName}.dlx";
        var dlqQueue = $"{queueName}.dlq";

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
            routingKey: dlqQueue,
            arguments: null);

        var queueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = dlxExchange,
            ["x-dead-letter-routing-key"] = dlqQueue
        };

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs);
    }
}
