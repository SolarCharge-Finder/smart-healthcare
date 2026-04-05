namespace AIService.Services;

/// <summary>
/// No-operation caching service that acts as a fallback when Redis is unavailable.
/// Allows the system to function normally without caching when Redis is not available.
/// </summary>
public class NoOpCachingService : ICachingService
{
    public Task<T?> GetAsync<T>(string key) where T : class => Task.FromResult<T?>(null);
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class => Task.CompletedTask;
    public Task RemoveAsync(string key) => Task.CompletedTask;
    public Task<bool> ExistsAsync(string key) => Task.FromResult(false);
    public Task FlushAsync() => Task.CompletedTask;
}
