using MediatR;
using OrderService.Application.DTOs;
using OrderService.Application.Requests;

namespace OrderService.Application.Commands
{
    public record CreateProductCommand(CreateProductRequest Request) : IRequest<ProductDto>;
}
