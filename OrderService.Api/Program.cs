using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderService.Api.Middleware;
using OrderService.Application;
using OrderService.Domain.Repositories;
using OrderService.Infrastructure;
using OrderService.Infrastructure.Auth;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplicationServices();

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("orders-testing-db"));
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddOrderMessagingAndIdempotency(builder.Configuration, builder.Environment);

    var testingJwtSettings = builder.Configuration.GetSection("JwtSettings");
    var testingJwtKey = testingJwtSettings["Key"] ?? "super-secret-key-nao-usar-em-producao";
    var testingJwtIssuer = testingJwtSettings["Issuer"] ?? "OrderService";
    var testingJwtAudience = testingJwtSettings["Audience"] ?? "OrderServiceUsers";
    var testingJwtExpirationMinutes = int.Parse(testingJwtSettings["ExpirationMinutes"] ?? "60");

    builder.Services.AddSingleton<IJwtTokenGenerator>(
        new JwtTokenGenerator(testingJwtKey, testingJwtIssuer, testingJwtAudience, testingJwtExpirationMinutes)
    );
}
else
{
    builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
}

#region JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var tokenKey = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(tokenKey),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "OrderService",
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"] ?? "OrderServiceUsers",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
#endregion

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Apply migrations on startup
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await dbContext.Database.MigrateAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderService API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseCors("AllowAll");
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();

app.MapControllers();

await app.RunAsync();

public partial class Program
{
    protected Program()
    {
    }
}
