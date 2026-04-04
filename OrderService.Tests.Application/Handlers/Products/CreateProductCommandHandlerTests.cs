using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using OrderService.Application.Commands;
using OrderService.Application.Handlers.Products;
using OrderService.Application.Requests;
using OrderService.Domain.Repositories;

namespace OrderService.Tests.Application.Handlers.Products
{
    public class CreateProductCommandHandlerTests
    {
        private static Mock<IValidator<ProductCommands>> BuildValidatorMock()
        {
            var validator = new Mock<IValidator<ProductCommands>>();
            validator
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<ProductCommands>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            return validator;
        }

        [Fact]
        public async Task Handle_WithValidDto_ShouldCreateProductAndReturnDto()
        {
            var unitOfWork = new Mock<IUnitOfWork>();
            var products = new Mock<IProductRepository>();
            var validator = BuildValidatorMock();

            unitOfWork.SetupGet(x => x.Products).Returns(products.Object);
            products
                .Setup(x => x.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = new ProductCommands(new CreateProductRequest
            {
                Name = "Widget",
                UnitPrice = 49.99m,
                AvailableQuantity = 50
            });

            var sut = new CreateProductCommandHandler(unitOfWork.Object, validator.Object);

            var result = await sut.Handle(command, CancellationToken.None);

            result.Name.Should().Be("Widget");
            result.UnitPrice.Should().Be(49.99m);
            result.AvailableQuantity.Should().Be(50);
            result.IsActive.Should().BeTrue();
            result.Id.Should().NotBeEmpty();

            products.Verify(x => x.AddAsync(It.IsAny<OrderService.Domain.Entities.Product>(), It.IsAny<CancellationToken>()), Times.Once);
            products.Verify(x => x.ExistsByNameAsync("Widget", It.IsAny<CancellationToken>()), Times.Once);
            unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenProductNameAlreadyExists_ShouldThrowValidationExceptionWithFriendlyMessage()
        {
            var unitOfWork = new Mock<IUnitOfWork>();
            var products = new Mock<IProductRepository>();
            var validator = BuildValidatorMock();

            unitOfWork.SetupGet(x => x.Products).Returns(products.Object);
            products.Setup(x => x.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var command = new ProductCommands(new CreateProductRequest
            {
                Name = "  GuiTar  ",
                UnitPrice = 1209.99m,
                AvailableQuantity = 10
            });

            var sut = new CreateProductCommandHandler(unitOfWork.Object, validator.Object);

            var act = () => sut.Handle(command, CancellationToken.None);

            var exception = await act.Should().ThrowAsync<ValidationException>();
            exception.Which.Message.Should().Be("A product with the same name already exists.");

            products.Verify(x => x.ExistsByNameAsync("GuiTar", It.IsAny<CancellationToken>()), Times.Once);
            products.Verify(x => x.AddAsync(It.IsAny<OrderService.Domain.Entities.Product>(), It.IsAny<CancellationToken>()), Times.Never);
            unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenValidationFails_ShouldThrowValidationException()
        {
            var unitOfWork = new Mock<IUnitOfWork>();
            var validator = new InlineValidator<ProductCommands>();
            validator.RuleFor(x => x.Request.Name).NotEmpty();

            var command = new ProductCommands(new CreateProductRequest
            {
                Name = "",
                UnitPrice = 10m,
                AvailableQuantity = 5
            });

            var sut = new CreateProductCommandHandler(unitOfWork.Object, validator);

            var act = () => sut.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();
        }
    }

}
