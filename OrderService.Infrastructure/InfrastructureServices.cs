using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.Repositories;
using OrderService.Infrastructure.Auth;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;

namespace OrderService.Infrastructure
{
    public static class InfrastructureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
               ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, b => b.MigrationsAssembly("OrderService.Infrastructure"))
            );

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            var jwtSettings = configuration.GetSection("JwtSettings");
            var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured");
            var jwtIssuer = jwtSettings["Issuer"] ?? "OrderService";
            var jwtAudience = jwtSettings["Audience"] ?? "OrderServiceUsers";
            var jwtExpirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

            services.AddSingleton<IJwtTokenGenerator>(
                new JwtTokenGenerator(jwtKey, jwtIssuer, jwtAudience, jwtExpirationMinutes));


            return services;
        }
    }
}
