using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Api.Controllers;
using OrderService.Api.Wrappers;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Requests;
using OrderService.Domain.Entities;

namespace OrderService.Tests.Api.Controllers
{
    public class ProductsControllerTests
    {
        #region Create
        [Fact]
        public async Task CreateProduct_ShouldReturnCreatedAtAction()
        {
            var mediator = new Mock<IMediator>();
            var logger = new Mock<ILogger<ProductsController>>();
            var product = Product.Create("Widget", 9.99m, 100);
            var productId = product.Id;

            mediator
                .Setup(x => x.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProductDto(product));

            var controller = new ProductsController(mediator.Object, logger.Object);

            var result = await controller.Create(new CreateProductRequest
            {
                Name = "Widget",
                UnitPrice = 9.99m,
                AvailableQuantity = 100
            }, CancellationToken.None);

            var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(ProductsController.GetById));
            created.Value.Should().BeOfType<Response<ProductDto>>()
                .Which.Data!.Id.Should().Be(productId);
        }

        [Fact]
        public async Task CreateProduct_WhenValidationFails_ShouldReturnBadRequest()
        {
            var mediator = new Mock<IMediator>();
            var logger = new Mock<ILogger<ProductsController>>();

            mediator
                .Setup(x => x.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ValidationException("UnitPrice must be greater than 0."));

            var controller = new ProductsController(mediator.Object, logger.Object);

            var result = await controller.Create(new CreateProductRequest
            {
                Name = "Widget",
                UnitPrice = 0m,
                AvailableQuantity = 10
            }, CancellationToken.None);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateProduct_WhenMediatorThrows_ShouldReturnInternalServerError()
        {
            var mediator = new Mock<IMediator>();
            var logger = new Mock<ILogger<ProductsController>>();

            mediator
                .Setup(x => x.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("unexpected error"));

            var controller = new ProductsController(mediator.Object, logger.Object);

            var result = await controller.Create(new CreateProductRequest
            {
                Name = "Widget",
                UnitPrice = 9.99m,
                AvailableQuantity = 10
            }, CancellationToken.None);

            result.Result.Should().BeOfType<ObjectResult>()
                .Which.StatusCode.Should().Be(500);
        }

        #endregion
    }
}
