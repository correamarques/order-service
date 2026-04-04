using FluentAssertions;
using FluentValidation;
using Moq;
using OrderService.Application.Handlers.Orders;
using OrderService.Application.Queries;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Tests.Application.Handlers.Orders;

public class GetOrderByIdQueryHandlerTests
{
    [Fact]
    public async Task GetOrderById_WhenMissing_ShouldThrowValidationException()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.Orders.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var sut = new GetOrderByIdQueryHandler(unitOfWork.Object);

        var act = () => sut.Handle(new GetOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
