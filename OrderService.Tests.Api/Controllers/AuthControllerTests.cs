using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Api.Controllers;
using OrderService.Infrastructure.Auth;

namespace OrderService.Tests.Api.Controllers;

public class AuthControllerTests
{
    [Fact]
    public void GetToken_WithEmptyEmail_ShouldReturnBadRequest()
    {
        var tokenGenerator = new Mock<IJwtTokenGenerator>();
        var logger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(tokenGenerator.Object, logger.Object);

        var result = controller.GetToken(new TokenRequest { Email = "" });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void GetToken_WithValidEmail_ShouldReturnOkWithToken()
    {
        var tokenGenerator = new Mock<IJwtTokenGenerator>();
        tokenGenerator.Setup(x => x.GenerateToken(It.IsAny<Guid>(), "user@example.com"))
            .Returns("token-value");

        var logger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(tokenGenerator.Object, logger.Object);

        var result = controller.GetToken(new TokenRequest { Email = "user@example.com" });

        result.Should().BeOfType<OkObjectResult>();
    }
}
