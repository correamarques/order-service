using FluentAssertions;
using OrderService.Domain;
using OrderService.Domain.Enums;
using OrderService.Tests.Domain.Builders;

namespace OrderService.Tests.Domain.Entities
{
    public class OrderTests
    {
        [Fact]
        public void Create_WithValidItems_ShouldCreateOrder()
        {
            var customerId = Guid.NewGuid();
            var items = new[]
            {
            new OrderItemBuilder().WithUnitPrice(100m).WithQuantity(2).Build(),
            new OrderItemBuilder().WithUnitPrice(50m).WithQuantity(1).Build()
        };

            var order = new OrderBuilder().WithCustomerId(customerId).WithCurrency("USD").WithItems(items).Build();

            order.CustomerId.Should().Be(customerId);
            order.Status.Should().Be(OrderStatus.Placed);
            order.Items.Should().HaveCount(2);
            order.Total.Should().Be(250m);
            order.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void Create_WithoutItems_ShouldThrowDomainException()
        {
            var act = () => new OrderBuilder().WithItems().Build();

            act.Should().Throw<DomainException>();
        }

        [Fact]
        public void Confirm_WhenAlreadyConfirmed_ShouldThrowDomainException()
        {
            var order = new OrderBuilder().Build();
            order.Confirm();

            var act = () => order.Confirm();

            act.Should().Throw<DomainException>();
        }

        [Fact]
        public void Cancel_WhenConfirmed_ShouldSetCanceledStatus()
        {
            var order = new OrderBuilder().Build();
            order.Confirm();

            order.Cancel();

            order.Status.Should().Be(OrderStatus.Canceled);
        }
    }

}
