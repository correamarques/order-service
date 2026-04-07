using System.Text.Json;
using StackExchange.Redis;

namespace OrderService.Infrastructure.Idempotency;

public class RedisIdempotencyCache(IConnectionMultiplexer connectionMultiplexer) : IIdempotencyCache
{
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<IdempotencyStoredResponse?> GetResponseAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(GetResponseKey(idempotencyKey));
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<IdempotencyStoredResponse>(value.ToString());
    }

    public Task ReleaseLockAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return _database.KeyDeleteAsync(GetLockKey(idempotencyKey));
    }

    public Task SetResponseAsync(string idempotencyKey, IdempotencyStoredResponse response, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        return _database.StringSetAsync(GetResponseKey(idempotencyKey), JsonSerializer.Serialize(response), ttl);
    }

    public Task<bool> TryAcquireLockAsync(string idempotencyKey, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        return _database.StringSetAsync(GetLockKey(idempotencyKey), "1", ttl, When.NotExists);
    }

    private static string GetLockKey(string idempotencyKey) => $"idem:lock:{idempotencyKey}";

    private static string GetResponseKey(string idempotencyKey) => $"idem:response:{idempotencyKey}";
}
