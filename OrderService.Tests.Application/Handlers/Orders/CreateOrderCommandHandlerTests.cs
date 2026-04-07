using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using OrderService.Application.Commands;
using OrderService.Application.Handlers.Orders;
using OrderService.Application.Requests;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Tests.Application.Handlers.Orders;

public class CreateOrderCommandHandlerTests
{
    private static Mock<IValidator<CreateOrderCommand>> BuildValidatorMock()
    {
        var validator = new Mock<IValidator<CreateOrderCommand>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<CreateOrderCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        return validator;
    }

    [Fact]
    public async Task Handle_WhenProductsAvailable_ShouldCreateOrderWithoutReservingStock()
    {
        var productId = Guid.NewGuid();
        var command = new CreateOrderCommand(new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Currency = "USD",
            Items = [new CreateOrderItemRequest { ProductId = productId, Quantity = 2 }]
        });

        var unitOfWork = new Mock<IUnitOfWork>();
        var orders = new Mock<IOrderRepository>();
        var outboxEvents = new Mock<IOutboxEventRepository>();
        var products = new Mock<IProductRepository>();
        var validator = BuildValidatorMock();

        unitOfWork.SetupGet(x => x.OutboxEvents).Returns(outboxEvents.Object);
        var product = Product.Create("Test", 20m, 10);
        typeof(Product).GetProperty(nameof(Product.Id))!.SetValue(product, productId);

        unitOfWork.SetupGet(x => x.Orders).Returns(orders.Object);
        unitOfWork.SetupGet(x => x.Products).Returns(products.Object);
        products.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new CreateOrderCommandHandler(unitOfWork.Object, validator.Object);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Total.Should().Be(40m);
        result.Status.Should().Be("Placed");
        orders.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        products.Verify(x => x.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAnyProductMissing_ShouldThrowValidationException()
    {
        var command = new CreateOrderCommand(new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Currency = "USD",
            Items = [new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 2 }]
        });

        var unitOfWork = new Mock<IUnitOfWork>();
        var validator = BuildValidatorMock();
        unitOfWork.SetupGet(x => x.OutboxEvents).Returns(new Mock<IOutboxEventRepository>().Object);
        unitOfWork.SetupGet(x => x.Products).Returns(new Mock<IProductRepository>().Object);
        unitOfWork.Setup(x => x.Products.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var sut = new CreateOrderCommandHandler(unitOfWork.Object, validator.Object);

        var act = () => sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WhenRequestedQuantityExceedsStock_ShouldThrowValidationException()
    {
        var productId = Guid.NewGuid();
        var command = new CreateOrderCommand(new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Currency = "USD",
            Items = [new CreateOrderItemRequest { ProductId = productId, Quantity = 5 }]
        });

        var unitOfWork = new Mock<IUnitOfWork>();
        var outboxEvents = new Mock<IOutboxEventRepository>();
        var products = new Mock<IProductRepository>();
        var validator = BuildValidatorMock();

        unitOfWork.SetupGet(x => x.OutboxEvents).Returns(outboxEvents.Object);
        var product = Product.Create("LowStock", 20m, 2);
        typeof(Product).GetProperty(nameof(Product.Id))!.SetValue(product, productId);

        unitOfWork.SetupGet(x => x.Products).Returns(products.Object);
        products.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);

        var sut = new CreateOrderCommandHandler(unitOfWork.Object, validator.Object);

        var act = () => sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Insufficient stock*");

        unitOfWork.Verify(x => x.Orders.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
