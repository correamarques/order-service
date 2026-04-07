using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.Repositories;

public class IdempotencyRecordRepository(AppDbContext context) : IIdempotencyRecordRepository
{
    private readonly AppDbContext _context = context;

    public async Task AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        await _context.IdempotencyRecords.AddAsync(record, cancellationToken);
    }

    public async Task<IdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.IdempotencyRecords
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public Task UpdateAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        _context.IdempotencyRecords.Update(record);
        return Task.CompletedTask;
    }
}
