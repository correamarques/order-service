using FluentValidation;
using MediatR;
using System.Text.Json;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Handlers.Orders;

public class CancelOrderCommandHandler(
    IUnitOfWork unitOfWork,
    IValidator<CancelOrderCommand> validator
) : IRequestHandler<CancelOrderCommand, OrderDto>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IValidator<CancelOrderCommand> _validator = validator;

    public async Task<OrderDto> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new ValidationException($"Order {request.OrderId} not found");

        if (order.Status == OrderStatus.Canceled)
        {
            return new OrderDto(order);
        }

        OrderStatus[] allowedStatus = [OrderStatus.Placed, OrderStatus.Confirmed];
        if (!allowedStatus.Contains(order.Status))
        {
            throw new ValidationException($"Only orders in 'Placed' or 'Confirmed' status can be canceled. Current status: {order.Status}");
        }

        var productIds = order.Items.Select(x => x.ProductId).ToList();
        var products = await _unitOfWork.Products.GetByIdsAsync(productIds, cancellationToken);

        foreach (var item in order.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product != null)
            {
                product.ReleaseStock(item.Quantity);
                await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
            }
        }

        order.Cancel();
        await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
        await _unitOfWork.OutboxEvents.AddAsync(
            OutboxEvent.Create(
                "order.canceled",
                JsonSerializer.Serialize(new
                {
                    order.Id,
                    order.CustomerId,
                    order.Status,
                    CanceledAt = order.UpdatedAt
                })),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrderDto(order);
    }
}
