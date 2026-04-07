using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.Repositories;

public class OutboxEventRepository(AppDbContext context) : IOutboxEventRepository
{
    private readonly AppDbContext _context = context;

    public async Task AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        await _context.OutboxEvents.AddAsync(outboxEvent, cancellationToken);
    }

    public async Task<List<OutboxEvent>> GetUnprocessedAsync(int take, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxEvents
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        _context.OutboxEvents.Update(outboxEvent);
        return Task.CompletedTask;
    }
}
