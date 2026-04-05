using StackExchange.Redis;
using System.Text.Json;

namespace AIService.Services;

public interface ICachingService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task FlushAsync();
}

public class CachingService : ICachingService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CachingService> _logger;
    private readonly IDatabase _db;
    private const string CacheKeyPrefix = "smarthealthcare:ai:";

    public CachingService(IConnectionMultiplexer redis, ILogger<CachingService> logger)
    {
        _redis = redis;
        _logger = logger;
        _db = _redis.GetDatabase();
        _logger.LogInformation("CachingService initialized with Redis connection");
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var prefixedKey = $"{CacheKeyPrefix}{key}";
            var value = await _db.StringGetAsync(prefixedKey);

            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(value.ToString());
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var prefixedKey = $"{CacheKeyPrefix}{key}";
            var serialized = JsonSerializer.Serialize(value);
            var timeout = expiration ?? TimeSpan.FromHours(24); // Default 24-hour expiration

            await _db.StringSetAsync(prefixedKey, serialized, timeout);
            _logger.LogDebug("Cached value for key: {Key}, expiration: {Expiration}", key, timeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            var prefixedKey = $"{CacheKeyPrefix}{key}";
            await _db.KeyDeleteAsync(prefixedKey);
            _logger.LogDebug("Removed cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var prefixedKey = $"{CacheKeyPrefix}{key}";
            return await _db.KeyExistsAsync(prefixedKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    public async Task FlushAsync()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            await server.FlushDatabaseAsync();
            _logger.LogInformation("Cache flushed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing cache");
        }
    }
}
