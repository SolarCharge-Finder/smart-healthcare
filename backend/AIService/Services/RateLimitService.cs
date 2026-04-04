using StackExchange.Redis;
using Serilog.Context;

namespace AIService.Services;

public interface IRateLimitService
{
    Task<(bool allowed, int remaining, int resetSeconds)> CheckRateLimitAsync(string userId, int maxRequests = 5, int windowSeconds = 3600);
    Task ResetUserLimitAsync(string userId);
}

/// <summary>
/// Redis-based rate limiter using sliding window algorithm
/// CRITICAL: Prevents API abuse and manages OpenAI costs
/// 
/// How it works:
/// 1. Each user has a Redis key: "ratelimit:{userId}"
/// 2. Stores list of timestamps when requests were made
/// 3. On new request, removes old timestamps outside the window
/// 4. Checks if remaining slots are available
/// 5. Returns remaining slots and time to reset
/// </summary>
public class RateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RateLimitService> _logger;
    private const string RateLimitKeyPrefix = "ratelimit:";

    public RateLimitService(IConnectionMultiplexer redis, ILogger<RateLimitService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Check if user has exceeded rate limit using sliding window
    /// </summary>
    public async Task<(bool allowed, int remaining, int resetSeconds)> CheckRateLimitAsync(
        string userId,
        int maxRequests = 5,
        int windowSeconds = 3600)
    {
        using (LogContext.PushProperty("RateLimitCheck", userId))
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = $"{RateLimitKeyPrefix}{userId}";
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var windowStart = now - windowSeconds;

                // Step 1: Get all requests for this user
                var values = await db.ListRangeAsync(key);
                
                // Step 2: Remove timestamps outside the window (older than windowSeconds)
                var validRequests = new List<long>();
                foreach (var value in values)
                {
                    if (long.TryParse(value.ToString(), out var timestamp) && timestamp > windowStart)
                    {
                        validRequests.Add(timestamp);
                    }
                }

                // Step 3: Check if user exceeded limit
                bool allowed = validRequests.Count < maxRequests;
                int remaining = Math.Max(0, maxRequests - validRequests.Count);

                // Step 4: Calculate time until reset (when oldest request falls out of window)
                int resetSeconds = 0;
                if (validRequests.Count > 0)
                {
                    var oldestRequest = validRequests.Min();
                    resetSeconds = Math.Max(0, (int)(oldestRequest + windowSeconds - now));
                }

                _logger.LogInformation(
                    "Rate limit check - UserId: {UserId}, Allowed: {Allowed}, " +
                    "Requests in window: {RequestsInWindow}/{MaxRequests}, " +
                    "Remaining: {Remaining}, Reset in: {ResetSeconds}s",
                    userId, allowed, validRequests.Count, maxRequests, remaining, resetSeconds);

                // Step 5: If allowed, record this request
                if (allowed)
                {
                    // Add current timestamp to the list
                    await db.ListLeftPushAsync(key, now);
                    
                    // Set expiry: keep Redis key for windowSeconds after oldest request
                    await db.KeyExpireAsync(key, TimeSpan.FromSeconds(windowSeconds));
                    
                    _logger.LogDebug("Request recorded for user {UserId}. New count: {Count}", 
                        userId, validRequests.Count + 1);
                }
                else
                {
                    _logger.LogWarning(
                        "Rate limit EXCEEDED for user {UserId}. " +
                        "Requests: {RequestCount}/{MaxRequests}. Reset in {ResetSeconds}s",
                        userId, validRequests.Count, maxRequests, resetSeconds);
                }

                return (allowed, remaining, resetSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for user {UserId}", userId);
                // Fail open - allow request if Redis fails
                return (true, -1, 0);
            }
        }
    }

    /// <summary>
    /// Reset rate limit for a user (admin action)
    /// </summary>
    public async Task ResetUserLimitAsync(string userId)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{RateLimitKeyPrefix}{userId}";
            await db.KeyDeleteAsync(key);
            _logger.LogInformation("Rate limit reset for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limit for user {UserId}", userId);
        }
    }
}
