namespace OrderService.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public string Password { get; set; } = "guest";
    public int Port { get; set; } = 5672;
    public string QueueName { get; set; } = "order-service-events";
    public string UserName { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
}
