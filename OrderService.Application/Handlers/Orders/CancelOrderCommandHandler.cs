using FluentValidation;
using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
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

        if (order.Status.ToString() == "Canceled")
        {
            return new OrderDto(order);
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrderDto(order);
    }
}
