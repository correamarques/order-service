namespace OrderService.Domain.Entities;

public class IdempotencyRecord
{
    private IdempotencyRecord() { }

    public string IdempotencyKey { get; private set; } = null!;
    public string Method { get; private set; } = null!;
    public string Endpoint { get; private set; } = null!;
    public string RequestHash { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public int? StatusCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public string? ContentType { get; private set; }
    public string? ResponseHeaders { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public static IdempotencyRecord CreatePending(
        string idempotencyKey,
        string method,
        string endpoint,
        string requestHash,
        DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new DomainException("Idempotency key is required");

        if (string.IsNullOrWhiteSpace(method))
            throw new DomainException("Request method is required");

        if (string.IsNullOrWhiteSpace(endpoint))
            throw new DomainException("Request endpoint is required");

        if (string.IsNullOrWhiteSpace(requestHash))
            throw new DomainException("Request hash is required");

        return new IdempotencyRecord
        {
            IdempotencyKey = idempotencyKey,
            Method = method,
            Endpoint = endpoint,
            RequestHash = requestHash,
            State = IdempotencyStates.InProgress,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    public bool Matches(string method, string endpoint, string requestHash)
    {
        return string.Equals(Method, method, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Endpoint, endpoint, StringComparison.Ordinal)
            && string.Equals(RequestHash, requestHash, StringComparison.Ordinal);
    }

    public bool IsCompleted => string.Equals(State, IdempotencyStates.Completed, StringComparison.Ordinal);

    public bool IsInProgress => string.Equals(State, IdempotencyStates.InProgress, StringComparison.Ordinal);

    public void Complete(int statusCode, string responseBody, string? contentType, string? responseHeaders)
    {
        State = IdempotencyStates.Completed;
        StatusCode = statusCode;
        ResponseBody = responseBody;
        ContentType = contentType;
        ResponseHeaders = responseHeaders;
        CompletedAt = DateTime.UtcNow;
    }

    public void ResetPending(DateTime expiresAt)
    {
        State = IdempotencyStates.InProgress;
        StatusCode = null;
        ResponseBody = null;
        ContentType = null;
        ResponseHeaders = null;
        CompletedAt = null;
        ExpiresAt = expiresAt;
    }

    public void MarkFailed()
    {
        State = IdempotencyStates.Failed;
        CompletedAt = DateTime.UtcNow;
    }

    public static class IdempotencyStates
    {
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}
