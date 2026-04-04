using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Api.Controllers;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Queries;
using OrderService.Application.Requests;
using OrderService.Application.Wrappers;
using OrderService.Domain.Entities;

namespace OrderService.Tests.Api.Controllers;

public class OrdersControllerTests
{
    #region Create
    [Fact]
    public async Task CreateOrder_ShouldReturnCreatedAtAction()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<OrdersController>>();
        var customerId = Guid.NewGuid();

        var order = Order.Create(
            customerId,
            "USD",
            [OrderItem.Create(Guid.NewGuid(), 10m, 1)]);

        mediator
            .Setup(x => x.Send(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderDto(order));

        var controller = new OrdersController(mediator.Object, logger.Object);

        var result = await controller.CreateOrder(new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Currency = "USD",
            Items = [new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 }]
        }, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(OrdersController.GetOrderById));
    }

    [Fact]
    public async Task CreateOrder_WhenMediatorThrows_ShouldReturnInternalServerError()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<OrdersController>>();

        mediator
            .Setup(x => x.Send(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var controller = new OrdersController(mediator.Object, logger.Object);

        var result = await controller.CreateOrder(new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Currency = "USD",
            Items = [new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 }]
        }, CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }
    #endregion

    #region Get
    [Fact]
    public async Task GetOrders_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<OrdersController>>();
        mediator
            .Setup(x => x.Send(It.IsAny<GetOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<OrderListItemDto>
            {
                Items =
                [
                    new OrderListItemDto
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = Guid.NewGuid(),
                        Status = "Placed",
                        Total = 10m,
                        CreatedAt = DateTime.UtcNow
                    }
                ],
                Total = 1,
                PageNumber = 1,
                PageSize = 10
            });
        var controller = new OrdersController(mediator.Object, logger.Object);
        var result = await controller.GetOrders(cancellationToken: CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetOrderById_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<OrdersController>>();
        var orderId = Guid.NewGuid();
        mediator
            .Setup(x => x.Send(It.IsAny<GetOrderByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderDto(Order.Create(Guid.NewGuid(), "USD", [OrderItem.Create(Guid.NewGuid(), 10m, 1)])));
        var controller = new OrdersController(mediator.Object, logger.Object);
        var result = await controller.GetOrderById(orderId, CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
    }
    #endregion
}
