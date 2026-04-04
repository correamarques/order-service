using MediatR;
using OrderService.Application.DTOs;
using OrderService.Application.Wrappers;

namespace OrderService.Application.Queries;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;

public record GetOrdersQuery(
    Guid? CustomerId = null,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PaginatedResult<OrderListItemDto>>;
