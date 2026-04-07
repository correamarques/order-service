using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace OrderService.Tests.Integration.Infrastructure;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__DefaultConnection";
    private const string JwtKeyEnvironmentVariable = "JwtSettings__Key";
    private const string JwtIssuerEnvironmentVariable = "JwtSettings__Issuer";
    private const string JwtAudienceEnvironmentVariable = "JwtSettings__Audience";
    private const string JwtExpirationEnvironmentVariable = "JwtSettings__ExpirationMinutes";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("orderservicedb")
        .WithUsername("postgres")
        .WithPassword("postgres123")
        .Build();

    private static readonly Uri ClientBaseAddress = new UriBuilder(Uri.UriSchemeHttps, "localhost").Uri;

    public OrderServiceWebApplicationFactory Factory { get; private set; } = null!;

    public HttpClient CreateClient()
    {
        return Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = ClientBaseAddress
        });
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, _database.GetConnectionString());
        Environment.SetEnvironmentVariable(JwtKeyEnvironmentVariable, "integration-tests-super-secret-key-1234567890");
        Environment.SetEnvironmentVariable(JwtIssuerEnvironmentVariable, "OrderService");
        Environment.SetEnvironmentVariable(JwtAudienceEnvironmentVariable, "OrderServiceUsers");
        Environment.SetEnvironmentVariable(JwtExpirationEnvironmentVariable, "60");

        Factory = new OrderServiceWebApplicationFactory(_database.GetConnectionString());

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.CanConnectAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.IdempotencyRecords.RemoveRange(await dbContext.IdempotencyRecords.ToListAsync());
        dbContext.OrderItems.RemoveRange(await dbContext.OrderItems.ToListAsync());
        dbContext.Orders.RemoveRange(await dbContext.Orders.ToListAsync());
        dbContext.OutboxEvents.RemoveRange(await dbContext.OutboxEvents.ToListAsync());
        dbContext.Products.RemoveRange(await dbContext.Products.ToListAsync());

        await dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        Environment.SetEnvironmentVariable(ConnectionStringEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(JwtKeyEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(JwtIssuerEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(JwtAudienceEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(JwtExpirationEnvironmentVariable, null);

        await _database.DisposeAsync();
    }
}
