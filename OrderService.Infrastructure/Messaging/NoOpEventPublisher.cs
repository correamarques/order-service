namespace OrderService.Infrastructure.Messaging;

public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
