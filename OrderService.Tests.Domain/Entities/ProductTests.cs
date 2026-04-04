using FluentAssertions;
using OrderService.Domain;
using OrderService.Tests.Domain.Builders;

namespace OrderService.Tests.Domain.Entities
{
    public class ProductTests
    {
        [Fact]
        public void Create_WithValidData_ShouldCreateProduct()
        {
            var product = new ProductBuilder()
                .WithName("Widget")
                .WithUnitPrice(10m)
                .WithAvailableQuantity(8)
                .Build();

            product.Name.Should().Be("Widget");
            product.AvailableQuantity.Should().Be(8);
            product.IsActive.Should().BeTrue();
        }

        [Fact]
        public void ReserveStock_WithInsufficientStock_ShouldThrowDomainException()
        {
            var product = new ProductBuilder().WithAvailableQuantity(2).Build();

            var act = () => product.ReserveStock(3);

            act.Should().Throw<DomainException>();
        }

        [Fact]
        public void ReserveAndReleaseStock_ShouldKeepExpectedQuantity()
        {
            var product = new ProductBuilder().WithAvailableQuantity(10).Build();

            product.ReserveStock(4);
            product.ReleaseStock(2);

            product.AvailableQuantity.Should().Be(8);
        }
    }
}
