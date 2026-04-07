namespace OrderService.Domain.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IIdempotencyRecordRepository IdempotencyRecords { get; }
    IOrderRepository Orders { get; }
    IOutboxEventRepository OutboxEvents { get; }
    IProductRepository Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
