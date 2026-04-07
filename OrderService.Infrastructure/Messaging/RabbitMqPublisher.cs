using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace OrderService.Infrastructure.Messaging;

public class RabbitMqPublisher(IOptions<RabbitMqOptions> options) : IEventPublisher
{
    private readonly RabbitMqOptions _options = options.Value;

    public Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Password = _options.Password,
            Port = _options.Port,
            UserName = _options.UserName,
            VirtualHost = _options.VirtualHost
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Type = eventType;

        var body = Encoding.UTF8.GetBytes(payload);
        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _options.QueueName,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }
}
