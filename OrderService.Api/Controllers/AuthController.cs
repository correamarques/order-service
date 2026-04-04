using Microsoft.AspNetCore.Mvc;
using OrderService.Infrastructure.Auth;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IJwtTokenGenerator tokenGenerator, ILogger<AuthController> logger) : ControllerBase
{
    private readonly IJwtTokenGenerator _tokenGenerator = tokenGenerator;
    private readonly ILogger<AuthController> _logger = logger;

    [HttpPost("token")]
    public IActionResult GetToken([FromBody] TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required");

        try
        {
            // * ATENÇÃO: Este é um exemplo simplificado.
            // Em um cenário real, o id do usuário seria recuperado do banco de dados após validar as credenciais
            // foi gerado um Guid aleatório para simular um usuário autenticado
            var userId = Guid.NewGuid();
            var token = _tokenGenerator.GenerateToken(userId, request.Email);

            return Ok(new { token, expiresIn = 3600 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating token");
            return StatusCode(500, "Internal server error");
        }
    }
}

public class TokenRequest
{
    public string Email { get; set; } = null!;
    public string? Password { get; set; }
}
