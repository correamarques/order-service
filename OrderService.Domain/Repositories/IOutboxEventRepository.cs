using OrderService.Domain.Entities;

namespace OrderService.Domain.Repositories;

public interface IOutboxEventRepository
{
    Task AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
    Task<List<OutboxEvent>> GetUnprocessedAsync(int take, CancellationToken cancellationToken = default);
    Task UpdateAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
}
