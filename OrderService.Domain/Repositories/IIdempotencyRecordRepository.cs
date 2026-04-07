using OrderService.Domain.Entities;

namespace OrderService.Domain.Repositories;

public interface IIdempotencyRecordRepository
{
    Task<IdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);
    Task UpdateAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);
}
