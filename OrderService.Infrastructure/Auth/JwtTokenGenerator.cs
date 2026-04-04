using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OrderService.Infrastructure.Auth;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email);
}

public class JwtTokenGenerator(string jwtKey, string jwtIssuer, string jwtAudience, int jwtExpirationMinutes = 60) : IJwtTokenGenerator
{
    private readonly string _jwtKey = jwtKey;
    private readonly string _jwtIssuer = jwtIssuer;
    private readonly string _jwtAudience = jwtAudience;
    private readonly int _jwtExpirationMinutes = jwtExpirationMinutes;

    public string GenerateToken(Guid userId, string email)
    {
        var tokenKey = Encoding.ASCII.GetBytes(_jwtKey);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
