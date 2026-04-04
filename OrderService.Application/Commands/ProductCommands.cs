using MediatR;
using OrderService.Application.DTOs;
using OrderService.Application.Requests;

namespace OrderService.Application.Commands
{
    public record ProductCommands(CreateProductRequest Request) : IRequest<ProductDto>;
}
