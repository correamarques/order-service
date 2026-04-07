namespace OrderService.Infrastructure.Idempotency;

public sealed class IdempotencyBeginResult
{
    private IdempotencyBeginResult(IdempotencyBeginStatus status, IdempotencyStoredResponse? response = null, string? message = null)
    {
        Status = status;
        Response = response;
        Message = message;
    }

    public IdempotencyBeginStatus Status { get; }
    public IdempotencyStoredResponse? Response { get; }
    public string? Message { get; }

    public static IdempotencyBeginResult Conflict(string message) => new(IdempotencyBeginStatus.Conflict, message: message);

    public static IdempotencyBeginResult InProgress(string message) => new(IdempotencyBeginStatus.InProgress, message: message);

    public static IdempotencyBeginResult Proceed() => new(IdempotencyBeginStatus.Proceed);

    public static IdempotencyBeginResult Replay(IdempotencyStoredResponse response) => new(IdempotencyBeginStatus.Replay, response);
}

public enum IdempotencyBeginStatus
{
    Proceed = 1,
    Replay = 2,
    Conflict = 3,
    InProgress = 4
}
