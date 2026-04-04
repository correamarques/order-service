using FluentValidation;
using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Handlers.Orders;

public class CreateOrderCommandHandler(
    IUnitOfWork unitOfWork,
    IValidator<CreateOrderCommand> validator) : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IValidator<CreateOrderCommand> _validator = validator;

    public async Task<OrderDto> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var request = command.Request;

        var productIds = request.Items.Select(x => x.ProductId).ToList();
        var products = await _unitOfWork.Products.GetByIdsAsync(productIds, cancellationToken);

        if (products.Count != request.Items.Count)
            throw new ValidationException("One or more products not found");

        // Validate stock and build order items
        var orderItems = new List<OrderItem>();
        foreach (var itemDto in request.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == itemDto.ProductId)
                ?? throw new ValidationException($"Product {itemDto.ProductId} not found");

            if (itemDto.Quantity > product.AvailableQuantity)
                throw new ValidationException(
                    $"Insufficient stock for product {product.Name}. Available: {product.AvailableQuantity}, Requested: {itemDto.Quantity}");

            var orderItem = OrderItem.Create(itemDto.ProductId, product.UnitPrice, itemDto.Quantity);
            orderItems.Add(orderItem);
        }

        var order = Order.Create(request.CustomerId, request.Currency, orderItems);
        await _unitOfWork.Orders.AddAsync(order, cancellationToken);

        // Reserve stock for each product
        foreach (var item in orderItems)
        {
            var product = products.First(p => p.Id == item.ProductId);
            product.ReserveStock(item.Quantity);
            await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrderDto(order);
    }
}
