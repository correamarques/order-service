using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using OrderService.Application.Commands;
using OrderService.Application.Handlers.Orders;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Repositories;

namespace OrderService.Tests.Application.Handlers.Orders;

public class ConfirmOrderCommandHandlerTests
{
    private static Mock<IValidator<ConfirmOrderCommand>> BuildConfirmValidatorMock()
    {
        var validator = new Mock<IValidator<ConfirmOrderCommand>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<ConfirmOrderCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        return validator;
    }

    [Fact]
    public async Task ConfirmOrder_WhenPlaced_ShouldUpdateAndPersist()
    {
        var productId = Guid.NewGuid();
        var order = Order.Create(Guid.NewGuid(), "USD", [OrderItem.Create(productId, 15m, 1)]);
        var orderId = Guid.NewGuid();
        var product = Product.Create("Test", 15m, 10);
        typeof(Product).GetProperty(nameof(Product.Id))!.SetValue(product, productId);

        var unitOfWork = new Mock<IUnitOfWork>();
        var orders = new Mock<IOrderRepository>();
        var products = new Mock<IProductRepository>();

        unitOfWork.SetupGet(x => x.Orders).Returns(orders.Object);
        unitOfWork.SetupGet(x => x.Products).Returns(products.Object);
        orders.Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        products.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new ConfirmOrderCommandHandler(unitOfWork.Object, BuildConfirmValidatorMock().Object);
        var result = await sut.Handle(new ConfirmOrderCommand(orderId), CancellationToken.None);

        result.Status.Should().Be("Confirmed");
        orders.Verify(x => x.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        products.Verify(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        products.Verify(x => x.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmOrder_WhenAlreadyConfirmed_ShouldNotUpdate()
    {
        var order = Order.Create(Guid.NewGuid(), "USD", [OrderItem.Create(Guid.NewGuid(), 15m, 1)]);
        order.Confirm();
        var orderId = Guid.NewGuid();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.Orders.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var sut = new ConfirmOrderCommandHandler(unitOfWork.Object, BuildConfirmValidatorMock().Object);
        var result = await sut.Handle(new ConfirmOrderCommand(orderId), CancellationToken.None);

        result.Status.Should().Be("Confirmed");
        unitOfWork.Verify(x => x.Orders.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [Trait("Category", "ConfirmOrder")]
    [InlineData(OrderStatus.Canceled)]
    public async Task ConfirmOrder_WhenStatusIsNotPlaced_ShouldThrowAndNotUpdate(OrderStatus status)
    {
        var order = Order.Create(Guid.NewGuid(), "USD", [OrderItem.Create(Guid.NewGuid(), 15m, 1)]);
        if (status == OrderStatus.Canceled)
        {
            order.Cancel();
        }

        var orderId = Guid.NewGuid();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.Orders.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var sut = new ConfirmOrderCommandHandler(unitOfWork.Object, BuildConfirmValidatorMock().Object);

        await FluentActions
            .Invoking(() => sut.Handle(new ConfirmOrderCommand(orderId), CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>();

        unitOfWork.Verify(x => x.Orders.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmOrder_WhenStockIsInsufficient_ShouldThrowAndNotPersist()
    {
        var productId = Guid.NewGuid();
        var order = Order.Create(Guid.NewGuid(), "USD", [OrderItem.Create(productId, 15m, 5)]);
        var orderId = Guid.NewGuid();
        var product = Product.Create("Test", 15m, 2);
        typeof(Product).GetProperty(nameof(Product.Id))!.SetValue(product, productId);

        var unitOfWork = new Mock<IUnitOfWork>();
        var products = new Mock<IProductRepository>();

        unitOfWork.Setup(x => x.Orders.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        unitOfWork.SetupGet(x => x.Products).Returns(products.Object);
        products.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        var sut = new ConfirmOrderCommandHandler(unitOfWork.Object, BuildConfirmValidatorMock().Object);

        var act = () => sut.Handle(new ConfirmOrderCommand(orderId), CancellationToken.None);

        await act.Should().ThrowAsync<OrderService.Domain.DomainException>();

        unitOfWork.Verify(x => x.Orders.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
