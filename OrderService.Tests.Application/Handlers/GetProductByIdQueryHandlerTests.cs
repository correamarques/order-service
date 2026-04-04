using FluentAssertions;
using Moq;
using OrderService.Application.Handlers.Products;
using OrderService.Application.Queries;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Tests.Domain.Builders;

namespace OrderService.Tests.Application.Handlers
{
    public class GetProductByIdQueryHandlerTests
    {
        [Fact]
        public async Task GetProductById_WhenMissing_ShouldThrowValidationException()
        {
            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(x => x.Products.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            var sut = new GetProductByIdQueryHandler(unitOfWork.Object);

            var act = () => sut.Handle(new GetProductByIdQuery(Guid.NewGuid()), CancellationToken.None);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GetProductById_WhenFound_ShouldReturnProductDto()
        {
            var product = new ProductBuilder().WithName("Gadget").WithUnitPrice(19.99m).Build();

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(x => x.Products.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            var sut = new GetProductByIdQueryHandler(unitOfWork.Object);

            var result = await sut.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

            result.Name.Should().Be("Gadget");
            result.UnitPrice.Should().Be(19.99m);
        }
    }
}
