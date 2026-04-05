using System.Text.Json;

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

    public EventPublisher(ILogger<EventPublisher> logger)
    {
        _logger = logger;
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
        var json = JsonSerializer.Serialize(@event);

        // Log event as JSON for debugging and audit trail
        _logger.LogInformation("Event: {EventType} | Correlation: {EventData}",
            eventType,
            json);

        // In production, this would:
        // 1. Publish to RabbitMQ exchange "smarthealthcare.ai" with routing key
        // 2. Or send to Azure Service Bus topic "symptom-analysis-events"
        // 3. Or use a message queue service (SQS, Kafka, etc.)
        // 
        // For now, logging provides event traceability and correlation ID propagation

        await Task.CompletedTask;
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
