using MediatR;
using OrderService.Application.DTOs;
using OrderService.Application.Wrappers;

namespace OrderService.Application.Queries
{
    public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

    public record GetProductsQuery(
        string? Name = null,
        int PageNumber = 1,
        int PageSize = 10
    ) : IRequest<PaginatedResult<ProductDto>>;
}
