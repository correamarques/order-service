using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.Repositories;
using OrderService.Infrastructure.Auth;
using OrderService.Infrastructure.Idempotency;
using OrderService.Infrastructure.Messaging;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;
using StackExchange.Redis;

namespace OrderService.Infrastructure;

public static class InfrastructureServices
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly("OrderService.Infrastructure"))
        );

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddOrderMessagingAndIdempotency(configuration, environment);

        var jwtSettings = configuration.GetSection("JwtSettings");
        var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var jwtIssuer = jwtSettings["Issuer"] ?? "OrderService";
        var jwtAudience = jwtSettings["Audience"] ?? "OrderServiceUsers";
        var jwtExpirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        services.AddSingleton<IJwtTokenGenerator>(
            new JwtTokenGenerator(jwtKey, jwtIssuer, jwtAudience, jwtExpirationMinutes));


        return services;
    }

    public static IServiceCollection AddOrderMessagingAndIdempotency(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSingleton(Options.Create(CreateIdempotencyOptions(configuration)));
        services.AddSingleton(Options.Create(CreateRabbitMqOptions(configuration)));
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        if (environment.IsEnvironment("Testing") || environment.IsEnvironment("IntegrationTesting"))
        {
            services.AddSingleton<IIdempotencyCache, InMemoryIdempotencyCache>();
            services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
            return services;
        }

        var redisConfiguration = configuration.GetSection("Redis")["Configuration"] ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConfiguration));
        services.AddSingleton<IIdempotencyCache, RedisIdempotencyCache>();
        services.AddSingleton<IEventPublisher, RabbitMqPublisher>();
        services.AddHostedService<OutboxProcessor>();

        return services;
    }

    private static IdempotencyOptions CreateIdempotencyOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(IdempotencyOptions.SectionName);
        return new IdempotencyOptions
        {
            LockTtlSeconds = int.TryParse(section["LockTtlSeconds"], out var lockTtlSeconds) ? lockTtlSeconds : 30,
            ResponseTtlMinutes = int.TryParse(section["ResponseTtlMinutes"], out var responseTtlMinutes) ? responseTtlMinutes : 60
        };
    }

    private static RabbitMqOptions CreateRabbitMqOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(RabbitMqOptions.SectionName);
        return new RabbitMqOptions
        {
            HostName = section["HostName"] ?? "localhost",
            Password = section["Password"] ?? "guest",
            Port = int.TryParse(section["Port"], out var port) ? port : 5672,
            QueueName = section["QueueName"] ?? "order-service-events",
            UserName = section["UserName"] ?? "guest",
            VirtualHost = section["VirtualHost"] ?? "/"
        };
    }
}
