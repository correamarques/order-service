using FluentAssertions;
using OrderService.Application.Commands;
using OrderService.Application.Requests;
using OrderService.Application.Validators;

namespace OrderService.Tests.Application.Validators.Orders;

public class CreateOrderCommandValidatorTests
{
    [Fact]
    public void CreateOrderCommandValidator_WithValidPayload_ShouldPass()
    {
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand(new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Currency = "USD",
            Items =
            [
                new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 }
            ]
        });

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateOrderCommandValidator_WithMissingItems_ShouldFail()
    {
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand(new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Currency = "USD",
            Items = []
        });

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

}
