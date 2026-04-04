using FluentAssertions;
using Moq;
using OrderService.Application.Handlers.Orders;
using OrderService.Application.Queries;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Tests.Application.Helpers;

namespace OrderService.Tests.Application.Handlers.Orders;

public class GetOrdersQueryHandlerTests
{
    [Fact]
    public async Task GetOrders_ShouldFilterByCustomerStatusAndPaginate()
    {
        var targetCustomer = Guid.NewGuid();

        var matching = Order.Create(targetCustomer, "USD", [OrderItem.Create(Guid.NewGuid(), 20m, 1)]);
        matching.Confirm();
        var otherCustomer = Order.Create(Guid.NewGuid(), "USD", [OrderItem.Create(Guid.NewGuid(), 30m, 1)]);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.Orders.GetQueryable())
            .Returns(AsyncQueryable.From([matching, otherCustomer]));

        var sut = new GetOrdersQueryHandler(unitOfWork.Object);

        var result = await sut.Handle(
            new GetOrdersQuery(CustomerId: targetCustomer, Status: "Confirmed", PageNumber: 1, PageSize: 10),
            CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
        result.Items[0].CustomerId.Should().Be(targetCustomer);
        result.Items[0].Status.Should().Be("Confirmed");
    }
}
