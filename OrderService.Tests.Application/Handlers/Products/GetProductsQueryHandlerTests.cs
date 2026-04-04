using FluentAssertions;
using Moq;
using OrderService.Application.Handlers.Products;
using OrderService.Application.Queries;
using OrderService.Domain.Repositories;
using OrderService.Tests.Application.Helpers;
using OrderService.Tests.Domain.Builders;

namespace OrderService.Tests.Application.Handlers.Products
{
    public class GetProductsQueryHandlerTests
    {
        [Fact]
        public async Task GetProducts_ShouldFilterByNameCaseInsensitive()
        {
            var widget = new ProductBuilder().WithName("Blue Widget").Build();
            var gadget = new ProductBuilder().WithName("Red Gadget").Build();
            var anotherWidget = new ProductBuilder().WithName("Green widget").Build();

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(x => x.Products.GetQueryable())
              .Returns(AsyncQueryable.From([widget, gadget, anotherWidget]));

            var sut = new GetProductsQueryHandler(unitOfWork.Object);

            var result = await sut.Handle(new GetProductsQuery(Name: "widget"), CancellationToken.None);

            result.Items.Should().HaveCount(2);
            result.Total.Should().Be(2);
            result.Items.Should().AllSatisfy(p => p.Name.ToLower().Should().Contain("widget"));
        }

        [Fact]
        public async Task GetProducts_ShouldTrimNameBeforeFiltering()
        {
            var widget = new ProductBuilder().WithName("Blue Widget").Build();
            var gadget = new ProductBuilder().WithName("Red Gadget").Build();

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(x => x.Products.GetQueryable())
              .Returns(AsyncQueryable.From([widget, gadget]));

            var sut = new GetProductsQueryHandler(unitOfWork.Object);

            var result = await sut.Handle(new GetProductsQuery(Name: "  widGet  "), CancellationToken.None);

            result.Items.Should().HaveCount(1);
            result.Total.Should().Be(1);
            result.Items[0].Name.Should().Be("Blue Widget");
        }

        [Fact]
        public async Task GetProducts_WithNullName_ShouldReturnAll()
        {
            var p1 = new ProductBuilder().WithName("Alpha").Build();
            var p2 = new ProductBuilder().WithName("Beta").Build();

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(x => x.Products.GetQueryable())
              .Returns(AsyncQueryable.From([p1, p2]));

            var sut = new GetProductsQueryHandler(unitOfWork.Object);

            var result = await sut.Handle(new GetProductsQuery(), CancellationToken.None);

            result.Items.Should().HaveCount(2);
            result.Total.Should().Be(2);
        }

        [Fact]
        public async Task GetProducts_ShouldPaginate()
        {
            var products = Enumerable.Range(1, 15)
                .Select(i => new ProductBuilder().WithName($"Product {i:D2}").Build())
                .ToList();

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(x => x.Products.GetQueryable())
              .Returns(AsyncQueryable.From(products));

            var sut = new GetProductsQueryHandler(unitOfWork.Object);

            var result = await sut.Handle(new GetProductsQuery(PageNumber: 2, PageSize: 5), CancellationToken.None);

            result.Items.Should().HaveCount(5);
            result.Total.Should().Be(15);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(5);
            result.Items.Select(x => x.Name).Should().Equal("Product 06", "Product 07", "Product 08", "Product 09", "Product 10");
        }
    }
}
