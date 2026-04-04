using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.DTOs;
using OrderService.Application.Queries;
using OrderService.Application.Wrappers;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Handlers.Products
{
    public class GetProductsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetProductsQuery, PaginatedResult<ProductDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<PaginatedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Products.GetQueryable();

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var normalizedName = request.Name.Trim().ToLower();
                query = query.Where(p => p.Name.Contains(normalizedName, StringComparison.CurrentCultureIgnoreCase));
            }

            var total = await query.CountAsync(cancellationToken);

            var products = await query
                .OrderBy(x => x.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedResult<ProductDto>
            {
                Items = [.. products.Select(p => new ProductDto(p))],
                Total = total,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}
