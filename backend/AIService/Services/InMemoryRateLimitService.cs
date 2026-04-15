using System.Collections.Concurrent;

namespace AIService.Services;

/// <summary>
/// In-memory rate limiting service using ConcurrentDictionary
/// Suitable for single-instance deployments and local development
/// For distributed systems, use RedisRateLimitService instead
/// </summary>
public sealed class InMemoryRateLimitService : IRateLimitService
{
    private sealed record RateLimitCounter(int Count, DateTimeOffset WindowStart);
    private readonly ConcurrentDictionary<string, RateLimitCounter> _counters = new();
    private readonly ILogger<InMemoryRateLimitService> _logger;

    public InMemoryRateLimitService(ILogger<InMemoryRateLimitService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if user has exceeded rate limit using sliding window algorithm
    /// </summary>
    public Task<(bool allowed, int remaining, int resetSeconds)> CheckRateLimitAsync(
        string userKey,
        int maxRequests = 5,
        int windowSeconds = 3600)
    {
        var now = DateTimeOffset.UtcNow;

        while (true)
        {
            // Get current counter or create new one if expired
            if (!_counters.TryGetValue(userKey, out var current) ||
                (now - current.WindowStart).TotalSeconds >= windowSeconds)
            {
                var fresh = new RateLimitCounter(1, now);
                _counters[userKey] = fresh;

                _logger.LogDebug(
                    "Rate limit window started for user {UserKey}. Max: {MaxRequests}",
                    userKey, maxRequests);

                return Task.FromResult((true, maxRequests - 1, windowSeconds));
            }

            // Increment counter
            var newCount = current.Count + 1;
            var updated = current with { Count = newCount };

            // Try to update atomically
            if (_counters.TryUpdate(userKey, updated, current))
            {
                var elapsed = (int)(now - current.WindowStart).TotalSeconds;
                var resetSeconds = Math.Max(0, windowSeconds - elapsed);
                var allowed = newCount <= maxRequests;
                var remaining = Math.Max(0, maxRequests - newCount);

                if (allowed)
                {
                    _logger.LogDebug(
                        "Rate limit check passed. User: {UserKey}, " +
                        "Requests: {Current}/{Max}, Remaining: {Remaining}, Reset in: {ResetSeconds}s",
                        userKey, newCount, maxRequests, remaining, resetSeconds);
                }
                else
                {
                    _logger.LogWarning(
                        "Rate limit EXCEEDED. User: {UserKey}, " +
                        "Requests: {Current}/{Max}, Reset in: {ResetSeconds}s",
                        userKey, newCount, maxRequests, resetSeconds);
                }

                return Task.FromResult((allowed, remaining, resetSeconds));
            }
        }
    }

    /// <summary>
    /// Reset rate limit for a user (admin operation)
    /// </summary>
    public Task ResetUserLimitAsync(string userKey)
    {
        _counters.TryRemove(userKey, out _);
        _logger.LogInformation("Rate limit reset for user {UserKey}", userKey);
        return Task.CompletedTask;
    }
}
