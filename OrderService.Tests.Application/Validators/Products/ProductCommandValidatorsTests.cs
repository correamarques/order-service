using FluentAssertions;
using OrderService.Application.Commands;
using OrderService.Application.Requests;
using OrderService.Application.Validators;

namespace OrderService.Tests.Application.Validators.Products
{
    public class ProductCommandValidatorsTests
    {
        [Fact]
        public void CreateProductCommandValidator_WithValidPayload_ShouldPass()
        {
            var validator = new CreateProductCommandValidator();
            var command = new ProductCommands(new CreateProductRequest
            {
                Name = "Widget",
                UnitPrice = 9.99m,
                AvailableQuantity = 100
            });

            var result = validator.Validate(command);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void CreateProductCommandValidator_WithEmptyName_ShouldFail()
        {
            var validator = new CreateProductCommandValidator();
            var command = new ProductCommands(new CreateProductRequest
            {
                Name = "",
                UnitPrice = 9.99m,
                AvailableQuantity = 10
            });

            var result = validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
        }

        [Fact]
        public void CreateProductCommandValidator_WithNegativePrice_ShouldFail()
        {
            var validator = new CreateProductCommandValidator();
            var command = new ProductCommands(new CreateProductRequest
            {
                Name = "Widget",
                UnitPrice = -1m,
                AvailableQuantity = 10
            });

            var result = validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName.Contains("UnitPrice"));
        }

        [Fact]
        public void CreateProductCommandValidator_WithZeroPrice_ShouldFail()
        {
            var validator = new CreateProductCommandValidator();
            var command = new ProductCommands(new CreateProductRequest
            {
                Name = "Widget",
                UnitPrice = 0m,
                AvailableQuantity = 10
            });

            var result = validator.Validate(command);

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void CreateProductCommandValidator_WithNegativeQuantity_ShouldFail()
        {
            var validator = new CreateProductCommandValidator();
            var command = new ProductCommands(new CreateProductRequest
            {
                Name = "Widget",
                UnitPrice = 9.99m,
                AvailableQuantity = -1
            });

            var result = validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName.Contains("AvailableQuantity"));
        }

        [Fact]
        public void CreateProductCommandValidator_WithZeroQuantity_ShouldPass()
        {
            var validator = new CreateProductCommandValidator();
            var command = new ProductCommands(new CreateProductRequest
            {
                Name = "Widget",
                UnitPrice = 9.99m,
                AvailableQuantity = 0
            });

            var result = validator.Validate(command);

            result.IsValid.Should().BeTrue();
        }
    }

}
