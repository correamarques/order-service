namespace OrderService.Domain.Entities;

public class OutboxEvent
{
    private OutboxEvent() { }

    public Guid Id { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    public static OutboxEvent Create(string eventType, string payload)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new DomainException("Event type is required");

        if (string.IsNullOrWhiteSpace(payload))
            throw new DomainException("Event payload is required");

        return new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }
}
