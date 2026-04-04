using FluentValidation;
using OrderService.Application.Commands;

namespace OrderService.Application.Validators;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Request.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required");

        RuleFor(x => x.Request.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(1, 3).WithMessage("Currency must be 1-3 characters");

        RuleFor(x => x.Request.Items)
            .NotEmpty().WithMessage("Order must have at least one item")
            .Must(x => x.All(item => item.Quantity > 0))
            .WithMessage("All items must have quantity greater than 0");

        RuleForEach(x => x.Request.Items)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.ProductId).NotEmpty();
                item.RuleFor(x => x.Quantity).GreaterThan(0);
            });
    }
}

public class ConfirmOrderCommandValidator : AbstractValidator<ConfirmOrderCommand>
{
    public ConfirmOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}
