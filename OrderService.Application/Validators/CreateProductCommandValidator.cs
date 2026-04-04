using FluentValidation;
using OrderService.Application.Commands;

namespace OrderService.Application.Validators
{
    public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator()
        {
            RuleFor(x => x.Request.Name)
                .NotEmpty().WithMessage("Product name is required.");

            RuleFor(x => x.Request.UnitPrice)
                .GreaterThan(0).WithMessage("UnitPrice must be greater than 0.");

            RuleFor(x => x.Request.AvailableQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Available quantity cannot be negative.");
        }
    }
}
