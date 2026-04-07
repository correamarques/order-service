namespace OrderService.Infrastructure.Idempotency;

public class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    public int ResponseTtlMinutes { get; set; } = 60;
    public int LockTtlSeconds { get; set; } = 30;
}
