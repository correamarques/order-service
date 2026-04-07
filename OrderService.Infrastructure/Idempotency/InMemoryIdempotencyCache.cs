using System.Collections.Concurrent;

namespace OrderService.Infrastructure.Idempotency;

public class InMemoryIdempotencyCache : IIdempotencyCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _responses = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _locks = new();

    public Task<IdempotencyStoredResponse?> GetResponseAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (_responses.TryGetValue(idempotencyKey, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return Task.FromResult<IdempotencyStoredResponse?>(entry.Response);
        }

        _responses.TryRemove(idempotencyKey, out _);
        return Task.FromResult<IdempotencyStoredResponse?>(null);
    }

    public Task ReleaseLockAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        _locks.TryRemove(idempotencyKey, out _);
        return Task.CompletedTask;
    }

    public Task SetResponseAsync(string idempotencyKey, IdempotencyStoredResponse response, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _responses[idempotencyKey] = new CacheEntry(response, DateTimeOffset.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    public Task<bool> TryAcquireLockAsync(string idempotencyKey, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
        while (true)
        {
            if (_locks.TryGetValue(idempotencyKey, out var current) && current <= DateTimeOffset.UtcNow)
            {
                _locks.TryRemove(idempotencyKey, out _);
                continue;
            }

            return Task.FromResult(_locks.TryAdd(idempotencyKey, expiresAt));
        }
    }

    private sealed record CacheEntry(IdempotencyStoredResponse Response, DateTimeOffset ExpiresAt);
}
