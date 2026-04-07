namespace OrderService.Infrastructure.Idempotency;

public sealed class IdempotencyStoredResponse
{
    public required string Body { get; init; }
    public string? ContentType { get; init; }
    public Dictionary<string, string[]> Headers { get; init; } = [];
    public int StatusCode { get; init; }
}
