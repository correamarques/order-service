using FluentAssertions;
using OrderService.Application.Commands;
using OrderService.Application.Validators;

namespace OrderService.Tests.Application.Validators.Orders;

public class ConfirmOrderCommandValidatorTests
{
    [Fact]
    public void ConfirmOrderCommandValidator_WithEmptyId_ShouldFail()
    {
        var validator = new ConfirmOrderCommandValidator();

        var result = validator.Validate(new ConfirmOrderCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }
}
