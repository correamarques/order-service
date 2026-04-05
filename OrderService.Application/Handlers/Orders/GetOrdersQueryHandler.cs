using MediatR;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using OrderService.Application.DTOs;
using OrderService.Application.Queries;
using OrderService.Application.Wrappers;
using OrderService.Domain.Enums;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Handlers.Orders;

public class GetOrdersQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetOrdersQuery, PaginatedResult<OrderListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<PaginatedResult<OrderListItemDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        if (request.PageNumber <= 0)
            throw new ValidationException("pageNumber must be greater than 0");

        if (request.PageSize <= 0)
            throw new ValidationException("pageSize must be greater than 0");

        var filtered = _unitOfWork.Orders.GetQueryable();

        if (request.CustomerId.HasValue && request.CustomerId != Guid.Empty)
            filtered = filtered.Where(o => o.CustomerId == request.CustomerId);

        if (!string.IsNullOrEmpty(request.Status))
        {
            if (Enum.TryParse<OrderStatus>(request.Status, true, out var parsedStatus))
                filtered = filtered.Where(o => o.Status == parsedStatus);
            else
                throw new ValidationException($"Invalid status '{request.Status}'. Allowed values: Placed, Confirmed, Canceled");
        }

        if (request.FromDate.HasValue)
            filtered = filtered.Where(o => o.CreatedAt >= request.FromDate);

        if (request.ToDate.HasValue)
            filtered = filtered.Where(o => o.CreatedAt <= request.ToDate.Value.AddDays(1));

        var total = await filtered.CountAsync(cancellationToken);

        var orders = await filtered
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<OrderListItemDto>
        {
            Items = [.. orders.Select(o => new OrderListItemDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                Status = o.Status.ToString(),
                Total = o.Total,
                CreatedAt = o.CreatedAt
            })],
            Total = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
