namespace OrderService.Infrastructure.Idempotency;

public sealed class IdempotencyRequest
{
    public required string Endpoint { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string Method { get; init; }
    public required string RequestHash { get; init; }
}
