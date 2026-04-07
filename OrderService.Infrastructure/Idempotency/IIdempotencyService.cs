namespace OrderService.Infrastructure.Idempotency;

public interface IIdempotencyService
{
    Task<IdempotencyBeginResult> BeginRequestAsync(IdempotencyRequest request, CancellationToken cancellationToken = default);
    Task CompleteRequestAsync(IdempotencyRequest request, IdempotencyStoredResponse response, CancellationToken cancellationToken = default);
    Task FailRequestAsync(IdempotencyRequest request, CancellationToken cancellationToken = default);
}
