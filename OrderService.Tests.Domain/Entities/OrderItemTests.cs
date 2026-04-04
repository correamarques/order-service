using FluentAssertions;
using OrderService.Domain;
using OrderService.Tests.Domain.Builders;

namespace OrderService.Tests.Domain.Entities
{
    public class OrderItemTests
    {
        [Fact]
        public void Create_WithEmptyProductId_ShouldThrowDomainException()
        {
            var act = () => new OrderItemBuilder().WithProductId(Guid.Empty).Build();

            act.Should().Throw<DomainException>();
        }

        [Fact]
        public void Create_WithNegativePrice_ShouldThrowDomainException()
        {
            var act = () => new OrderItemBuilder().WithUnitPrice(-1m).Build();

            act.Should().Throw<DomainException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithNonPositiveQuantity_ShouldThrowDomainException(int quantity)
        {
            var act = () => new OrderItemBuilder().WithQuantity(quantity).Build();

            act.Should().Throw<DomainException>();
        }
    }

}
