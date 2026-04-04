using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using OrderService.Application.Commands;
using OrderService.Application.Handlers.Orders;
using OrderService.Domain.Entities;
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
    public async Task CancelOrder_WhenAlreadyCanceled_ShouldReturnWithoutUpdate()
    {
        var order = Order.Create(Guid.NewGuid(), "USD", [OrderItem.Create(Guid.NewGuid(), 15m, 1)]);
        var orderId = Guid.NewGuid();
        order.Cancel();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.Orders.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var sut = new CancelOrderCommandHandler(unitOfWork.Object, BuildCancelValidatorMock().Object);
        var result = await sut.Handle(new CancelOrderCommand(orderId), CancellationToken.None);

        result.Status.Should().Be("Canceled");
        unitOfWork.Verify(x => x.Orders.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
