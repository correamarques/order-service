using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace OrderService.Tests.Integration.Infrastructure;

public sealed class OrderServiceWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    private readonly string _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["JwtSettings:Key"] = "integration-tests-super-secret-key-1234567890",
                ["JwtSettings:Issuer"] = "OrderService",
                ["JwtSettings:Audience"] = "OrderServiceUsers",
                ["JwtSettings:ExpirationMinutes"] = "60"
            });
        });
    }
}
