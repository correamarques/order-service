using FluentValidation;
using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Domain.Enums;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Handlers.Orders;


public class ConfirmOrderCommandHandler(
    IUnitOfWork unitOfWork,
    IValidator<ConfirmOrderCommand> validator
) : IRequestHandler<ConfirmOrderCommand, OrderDto>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IValidator<ConfirmOrderCommand> _validator = validator;

    public async Task<OrderDto> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new ValidationException($"Order {request.OrderId} not found");

        if (order.Status == OrderStatus.Confirmed)
        {
            return new OrderDto(order);
        }

        if (order.Status != OrderStatus.Placed)
        {
            throw new ValidationException($"Only orders in 'Placed' status can be confirmed. Current status: {order.Status}");
        }

        order.Confirm();
        await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrderDto(order);
    }
}
