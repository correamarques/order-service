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

public class CancelOrderCommandHandlerTests
{
    private static Mock<IValidator<CancelOrderCommand>> BuildCancelValidatorMock()
    {
        var validator = new Mock<IValidator<CancelOrderCommand>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<CancelOrderCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        return validator;
    }

    [Fact]
    public async Task CancelOrder_WhenPlaced_ShouldUpdateAndPersist()
    {
        var order = Order.Create(Guid.NewGuid(), "USD", [OrderItem.Create(Guid.NewGuid(), 15m, 1)]);
        var orderId = Guid.NewGuid();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.OutboxEvents).Returns(new Mock<IOutboxEventRepository>().Object);
        unitOfWork.Setup(x => x.Orders.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        unitOfWork.Setup(x => x.Products.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new CancelOrderCommandHandler(unitOfWork.Object, BuildCancelValidatorMock().Object);
        var result = await sut.Handle(new CancelOrderCommand(orderId), CancellationToken.None);

        result.Status.Should().Be("Canceled");
        unitOfWork.Verify(x => x.Orders.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelOrder_WhenAlreadyCanceled_ShouldReturnWithoutUpdate()
    {
        var order = Order.Create(Guid.NewGuid(), "USD", [OrderItem.Create(Guid.NewGuid(), 15m, 1)]);
        var orderId = Guid.NewGuid();
        order.Cancel();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.OutboxEvents).Returns(new Mock<IOutboxEventRepository>().Object);
        unitOfWork.Setup(x => x.Orders.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var sut = new CancelOrderCommandHandler(unitOfWork.Object, BuildCancelValidatorMock().Object);
        var result = await sut.Handle(new CancelOrderCommand(orderId), CancellationToken.None);

        result.Status.Should().Be("Canceled");
        unitOfWork.Verify(x => x.Orders.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_WhenCalledTwice_ShouldBeIdempotent()
    {
        var order = Order.Create(Guid.NewGuid(), "USD", [OrderItem.Create(Guid.NewGuid(), 15m, 1)]);
        var orderId = Guid.NewGuid();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.OutboxEvents).Returns(new Mock<IOutboxEventRepository>().Object);
        unitOfWork.Setup(x => x.Orders.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        unitOfWork.Setup(x => x.Products.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new CancelOrderCommandHandler(unitOfWork.Object, BuildCancelValidatorMock().Object);

        var first = await sut.Handle(new CancelOrderCommand(orderId), CancellationToken.None);
        var second = await sut.Handle(new CancelOrderCommand(orderId), CancellationToken.None);

        first.Status.Should().Be("Canceled");
        second.Status.Should().Be("Canceled");
        unitOfWork.Verify(x => x.Orders.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
