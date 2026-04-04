using FluentValidation;
using MediatR;
using OrderService.Application.DTOs;
using OrderService.Application.Queries;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Handlers.Orders;

public class GetOrderByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new ValidationException($"Order {request.OrderId} not found");

        return new OrderDto(order);
    }
}

