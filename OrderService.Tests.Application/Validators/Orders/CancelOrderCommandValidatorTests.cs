using FluentAssertions;
using OrderService.Application.Commands;
using OrderService.Application.Validators;

namespace OrderService.Tests.Application.Validators.Orders;

public class CancelOrderCommandValidatorTests
{

    [Fact]
    public void CancelOrderCommandValidator_WithEmptyId_ShouldFail()
    {
        var validator = new CancelOrderCommandValidator();

        var result = validator.Validate(new CancelOrderCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }
}
