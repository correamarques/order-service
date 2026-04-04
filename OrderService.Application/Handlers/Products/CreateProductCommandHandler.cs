using FluentValidation;
using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Handlers.Products
{
    public class CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        IValidator<ProductCommands> validator) : IRequestHandler<ProductCommands, ProductDto>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IValidator<ProductCommands> _validator = validator;

        private const string DuplicateProductNameError = "A product with the same name already exists.";

        public async Task<ProductDto> Handle(ProductCommands command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var request = command.Request;

            var normalizedName = request.Name.Trim();

            if (await _unitOfWork.Products.ExistsByNameAsync(normalizedName, cancellationToken))
            {
                throw new ValidationException(DuplicateProductNameError);
            }

            var product = Product.Create(normalizedName, request.UnitPrice, request.AvailableQuantity);
            await _unitOfWork.Products.AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ProductDto(product);
        }
    }
}
