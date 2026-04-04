using MediatR;
using OrderService.Application.DTOs;
using OrderService.Application.Requests;

namespace OrderService.Application.Commands;

public record CreateOrderCommand(CreateOrderRequest Request) : IRequest<OrderDto>;
public record ConfirmOrderCommand(Guid OrderId) : IRequest<OrderDto>;
public record CancelOrderCommand(Guid OrderId) : IRequest<OrderDto>;
