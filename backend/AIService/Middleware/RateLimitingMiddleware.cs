using Serilog.Context;

namespace AIService.Middleware;

/// <summary>
/// Middleware to enforce rate limiting on AI endpoints
/// Blocks requests that exceed the limit and returns 429 (Too Many Requests)
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly int _maxRequests;
    private readonly int _windowSeconds;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _maxRequests = 5;          // Max 5 calls
        _windowSeconds = 3600;     // Per hour
    }

    public async Task InvokeAsync(HttpContext context, IRateLimitService rateLimitService)
    {
        // Only apply rate limiting to AI endpoints
        var path = context.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/ai/"))
        {
            await _next(context);
            return;
        }

        using (LogContext.PushProperty("Middleware", "RateLimiting"))
        {
            try
            {
                // Get user ID from claims or IP address as fallback
                var userId = ExtractUserId(context);

                _logger.LogDebug("Rate limit check for endpoint {Path} by user {UserId}", path, userId);

                // Check rate limit
                var (allowed, remaining, resetSeconds) = await rateLimitService.CheckRateLimitAsync(
                    userId, _maxRequests, _windowSeconds);

                // Add rate limit headers
                context.Response.Headers["X-RateLimit-Limit"] = _maxRequests.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
                context.Response.Headers["X-RateLimit-Reset"] = (DateTimeOffset.UtcNow.AddSeconds(resetSeconds)).ToUnixTimeSeconds().ToString();

                if (!allowed)
                {
                    _logger.LogWarning(
                        "Rate limit exceeded for user {UserId}. " +
                        "Limit: {MaxRequests}/{WindowSeconds}s, Reset in: {ResetSeconds}s",
                        userId, _maxRequests, _windowSeconds, resetSeconds);

                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.ContentType = "application/json";

                    var errorResponse = new
                    {
                        error = "Rate limit exceeded",
                        message = $"You have exceeded your API limit of {_maxRequests} calls per hour.",
                        retryAfter = resetSeconds,
                        resetAt = DateTimeOffset.UtcNow.AddSeconds(resetSeconds).UtcDateTime
                    };

                    await context.Response.WriteAsJsonAsync(errorResponse);
                    return;
                }

                _logger.LogInformation("Rate limit check passed. Remaining: {Remaining}/{Max}", remaining, _maxRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in rate limiting middleware");
                // Fail open - allow the request
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Extract user ID from JWT claims or fall back to IP address
    /// </summary>
    private string ExtractUserId(HttpContext context)
    {
        // Try to get from JWT token in Authorization header
        var userId = context.User?.FindFirst("sub")?.Value 
            ?? context.User?.FindFirst("UserId")?.Value
            ?? context.User?.FindFirst("id")?.Value;

        if (!string.IsNullOrEmpty(userId))
            return userId;

        // Fallback: use IP address for anonymous users
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip-{ipAddress}";
    }
}

/// <summary>
/// Extension method to register rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitingMiddleware>();
    }
}
