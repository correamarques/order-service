namespace OrderService.Api.Middleware;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentRequestAttribute : Attribute
{
}
