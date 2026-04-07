using OrderService.Domain.Repositories;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.Repositories;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private readonly AppDbContext _context = context;
    private IIdempotencyRecordRepository? _idempotencyRecords;
    private IOrderRepository? _orders;
    private IOutboxEventRepository? _outboxEvents;
    private IProductRepository? _products;

    public IIdempotencyRecordRepository IdempotencyRecords => _idempotencyRecords ??= new IdempotencyRecordRepository(_context);
    public IOrderRepository Orders => _orders ??= new OrderRepository(_context);
    public IOutboxEventRepository OutboxEvents => _outboxEvents ??= new OutboxEventRepository(_context);
    public IProductRepository Products => _products ??= new ProductRepository(_context);

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
