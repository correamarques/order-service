using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using OrderService.Infrastructure.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace OrderService.Tests.Infrastructure.Auth;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_ShouldContainExpectedClaimsAndMetadata()
    {
        const string key = "your-super-secret-key-that-is-very-long-and-secure-please-change-this-in-production";
        const string issuer = "OrderService";
        const string audience = "OrderServiceUsers";

        var generator = new JwtTokenGenerator(key, issuer, audience, 60);
        var userId = Guid.NewGuid();

        var token = generator.GenerateToken(userId, "fabian@teste.com");

        var handler = new JwtSecurityTokenHandler();
        var validation = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        }, out _);

        validation.FindFirst(JwtRegisteredClaimNames.Sub)?.Value.Should().Be(userId.ToString());
        validation.FindFirst(JwtRegisteredClaimNames.Email)?.Value.Should().Be("fabian@teste.com");
    }
}
