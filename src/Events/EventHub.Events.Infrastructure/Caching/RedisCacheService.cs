using System.Text.Json;
using EventHub.Events.Application.Abstractions.Caching;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EventHub.Events.Infrastructure.Caching;

public sealed class RedisCacheService(IConnectionMultiplexer multiplexer, ILogger<RedisCacheService> logger) : ICacheService
{
    private IDatabase Database => multiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            RedisValue value = await Database.StringGetAsync(key);

            return value.HasValue
                ? JsonSerializer.Deserialize<T>(value.ToString())
                : default;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Cached value for key {CacheKey} is corrupted", key);

            return default;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis get failed for key {CacheKey}", key);

            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        try
        {
            string payload = JsonSerializer.Serialize(value);

            await Database.StringSetAsync(key, payload, ttl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis set failed for key {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await Database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis remove failed for key {CacheKey}", key);
        }
    }
}
