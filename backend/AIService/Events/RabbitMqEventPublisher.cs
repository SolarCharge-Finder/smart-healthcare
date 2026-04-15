using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace AIService.Events;

public interface IEventPublisher
{
    Task PublishAnalysisCompletedAsync(SymptomAnalysisCompletedEvent @event);
    Task PublishAnalysisFallbackAsync(SymptomAnalysisFallbackEvent @event);
    Task PublishAnalysisCachedAsync(SymptomAnalysisCachedEvent @event);
}

/// <summary>
/// Event publisher that logs events for integration with message bus
/// In production, this would publish to RabbitMQ/Azure Service Bus
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly ILogger<EventPublisher> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConnectionFactory _connectionFactory;
    private readonly string _exchange;
    private readonly string _deadLetterExchange;
    private readonly string _deadLetterQueue;
    private readonly int _maxRetries;
    private readonly int _retryDelayMs;

    public EventPublisher(ILogger<EventPublisher> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        _exchange = _configuration["RabbitMQ:Exchange"] ?? "smarthealthcare.ai";
        _deadLetterExchange = _configuration["RabbitMQ:DeadLetterExchange"] ?? "smarthealthcare.ai.dlx";
        _deadLetterQueue = _configuration["RabbitMQ:DeadLetterQueue"] ?? "smarthealthcare.ai.deadletter";
        _maxRetries = int.TryParse(_configuration["RabbitMQ:PublishMaxRetries"], out var retries) ? Math.Max(1, retries) : 3;
        _retryDelayMs = int.TryParse(_configuration["RabbitMQ:RetryDelayMs"], out var delayMs) ? Math.Max(100, delayMs) : 500;
        _connectionFactory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = _configuration["RabbitMQ:User"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672
        };
    }

    public async Task PublishAnalysisCompletedAsync(SymptomAnalysisCompletedEvent @event)
    {
        try
        {
            await PublishEventAsync("SymptomAnalysisCompleted", @event);
            _logger.LogInformation(
                "Published SymptomAnalysisCompletedEvent. CorrelationId: {CorrelationId}, Success: {Success}, Cost: ${Cost}",
                @event.CorrelationId,
                @event.Success,
                @event.CostUsd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish SymptomAnalysisCompletedEvent. CorrelationId: {CorrelationId}",
                @event.CorrelationId);
        }
    }

    public async Task PublishAnalysisFallbackAsync(SymptomAnalysisFallbackEvent @event)
    {
        try
        {
            await PublishEventAsync("SymptomAnalysisFallback", @event);
            _logger.LogWarning(
                "Published SymptomAnalysisFallbackEvent. CorrelationId: {CorrelationId}, Reason: {Reason}",
                @event.CorrelationId,
                @event.FailureReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish SymptomAnalysisFallbackEvent. CorrelationId: {CorrelationId}",
                @event.CorrelationId);
        }
    }

    public async Task PublishAnalysisCachedAsync(SymptomAnalysisCachedEvent @event)
    {
        try
        {
            await PublishEventAsync("SymptomAnalysisCached", @event);
            _logger.LogDebug("Published SymptomAnalysisCachedEvent. CacheKey: {CacheKey}", @event.CacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish SymptomAnalysisCachedEvent. CorrelationId: {CorrelationId}",
                @event.CorrelationId);
        }
    }

    private async Task PublishEventAsync<T>(string eventType, T @event)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = eventType,
            occurredAt = DateTime.UtcNow,
            data = @event
        });

        for (var attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                await channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, durable: true, autoDelete: false);
                await channel.ExchangeDeclareAsync(_deadLetterExchange, ExchangeType.Fanout, durable: true, autoDelete: false);
                await channel.QueueDeclareAsync(_deadLetterQueue, durable: true, exclusive: false, autoDelete: false);
                await channel.QueueBindAsync(_deadLetterQueue, _deadLetterExchange, routingKey: string.Empty);

                var routingKey = $"ai.{eventType}".ToLowerInvariant();
                var body = Encoding.UTF8.GetBytes(payload);

                await channel.BasicPublishAsync(
                    exchange: _exchange,
                    routingKey: routingKey,
                    mandatory: false,
                    body: body);

                _logger.LogInformation(
                    "Published event {EventType} to exchange {Exchange} with routing key {RoutingKey}. Attempt: {Attempt}",
                    eventType,
                    _exchange,
                    routingKey,
                    attempt);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ publish attempt failed. EventType: {EventType}, Attempt: {Attempt}/{MaxRetries}",
                    eventType,
                    attempt,
                    _maxRetries);

                if (attempt < _maxRetries)
                {
                    await Task.Delay(_retryDelayMs);
                }
            }
        }

        await TryPublishToDeadLetterAsync(eventType, payload);
    }

    private async Task TryPublishToDeadLetterAsync(string eventType, string payload)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(_deadLetterExchange, ExchangeType.Fanout, durable: true, autoDelete: false);
            await channel.QueueDeclareAsync(_deadLetterQueue, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(_deadLetterQueue, _deadLetterExchange, routingKey: string.Empty);

            var body = Encoding.UTF8.GetBytes(payload);
            await channel.BasicPublishAsync(
                exchange: _deadLetterExchange,
                routingKey: string.Empty,
                mandatory: false,
                body: body);

            _logger.LogWarning(
                "Published event {EventType} to dead-letter queue {DeadLetterQueue} after retry exhaustion.",
                eventType,
                _deadLetterQueue);
        }
        catch (Exception ex)
        {
            // Non-blocking behavior: never throw to API request path.
            _logger.LogError(ex,
                "Failed to publish event {EventType} to RabbitMQ and dead-letter queue. Event payload logged for replay.",
                eventType);
            _logger.LogInformation("Unpublished event payload: {Payload}", payload);
        }
    }
}

/// <summary>
/// No-op event publisher fallback (used when message bus is unavailable)
/// </summary>
public class NoOpEventPublisher : IEventPublisher
{
    private readonly ILogger<NoOpEventPublisher> _logger;

    public NoOpEventPublisher(ILogger<NoOpEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAnalysisCompletedAsync(SymptomAnalysisCompletedEvent @event)
    {
        _logger.LogDebug("Event publishing disabled (NoOp). Event: SymptomAnalysisCompleted, CorrelationId: {CorrelationId}",
            @event.CorrelationId);
        return Task.CompletedTask;
    }

    public Task PublishAnalysisFallbackAsync(SymptomAnalysisFallbackEvent @event)
    {
        _logger.LogDebug("Event publishing disabled (NoOp). Event: SymptomAnalysisFallback, CorrelationId: {CorrelationId}",
            @event.CorrelationId);
        return Task.CompletedTask;
    }

    public Task PublishAnalysisCachedAsync(SymptomAnalysisCachedEvent @event)
    {
        _logger.LogDebug("Event publishing disabled (NoOp). Event: SymptomAnalysisCached, CorrelationId: {CorrelationId}",
            @event.CorrelationId);
        return Task.CompletedTask;
    }
}
