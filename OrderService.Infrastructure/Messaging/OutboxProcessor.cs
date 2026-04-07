using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Repositories;

namespace OrderService.Infrastructure.Messaging;

public class OutboxProcessor(ILogger<OutboxProcessor> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly ILogger<OutboxProcessor> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var events = await unitOfWork.OutboxEvents.GetUnprocessedAsync(50, cancellationToken);
        if (events.Count == 0)
        {
            return;
        }

        foreach (var outboxEvent in events)
        {
            await publisher.PublishAsync(outboxEvent.EventType, outboxEvent.Payload, cancellationToken);
            outboxEvent.MarkProcessed();
            await unitOfWork.OutboxEvents.UpdateAsync(outboxEvent, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
