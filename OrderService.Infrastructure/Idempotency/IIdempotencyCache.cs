namespace OrderService.Infrastructure.Idempotency;

public interface IIdempotencyCache
{
    Task<IdempotencyStoredResponse?> GetResponseAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task ReleaseLockAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task SetResponseAsync(string idempotencyKey, IdempotencyStoredResponse response, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<bool> TryAcquireLockAsync(string idempotencyKey, TimeSpan ttl, CancellationToken cancellationToken = default);
}
